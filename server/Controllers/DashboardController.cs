using GestaoFinanceiraPessoal.Api.Contracts.Dashboard;
using GestaoFinanceiraPessoal.Api.Contracts.Investments;
using GestaoFinanceiraPessoal.Api.Contracts.Transactions;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Enums;
using GestaoFinanceiraPessoal.Api.Extensions;
using GestaoFinanceiraPessoal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(
    ApplicationDbContext dbContext,
    RecurringTransactionService recurringTransactionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;

        if (selectedYear is < 2000 or > 2200 || selectedMonth is < 1 or > 12)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "O período informado é inválido.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userId = User.GetRequiredUserId();
        var profile = await dbContext.FinancialProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conclua o onboarding antes de acessar o dashboard.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var firstDay = new DateOnly(selectedYear, selectedMonth, 1);
        var lastDay = new DateOnly(
            selectedYear,
            selectedMonth,
            DateTime.DaysInMonth(selectedYear, selectedMonth));

        await recurringTransactionService.MaterializeThroughAsync(
            userId,
            lastDay,
            cancellationToken);

        var monthlyTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Category)
            .Include(transaction => transaction.RecurringTransaction)
            .Where(transaction => transaction.UserId == userId
                && transaction.Date >= firstDay
                && transaction.Date <= lastDay)
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);

        var confirmedMonthlyTransactions = monthlyTransactions
            .Where(transaction => transaction.Status == TransactionStatus.Confirmed)
            .ToList();

        var monthlyIncome = confirmedMonthlyTransactions
            .Where(transaction => transaction.Type == TransactionType.Income)
            .Sum(transaction => transaction.Amount);

        var monthlyExpense = confirmedMonthlyTransactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .Sum(transaction => transaction.Amount);

        var monthlyIncomeRecorded = confirmedMonthlyTransactions
            .Any(transaction => transaction.Type == TransactionType.Income && transaction.IsMonthlyIncome);

        var confirmedIncome = await dbContext.Transactions
            .Where(transaction => transaction.UserId == userId
                && transaction.Type == TransactionType.Income
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Date <= today)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;

        var confirmedExpense = await dbContext.Transactions
            .Where(transaction => transaction.UserId == userId
                && transaction.Type == TransactionType.Expense
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Date <= today)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;

        var incomeReminderDue = profile.IncomeSetupMode == IncomeSetupMode.MonthlyReminder
            && selectedYear == today.Year
            && selectedMonth == today.Month
            && profile.MonthlyIncomeReminderDay.HasValue
            && today.Day >= profile.MonthlyIncomeReminderDay.Value
            && !monthlyIncomeRecorded;

        var recentTransactions = monthlyTransactions
            .Take(10)
            .Select(TransactionResponse.FromEntity)
            .ToList();

        var totalBalance = profile.InitialCapital + confirmedIncome - confirmedExpense;
        var netInvestmentMovements = await dbContext.InvestmentMovements
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Date <= today)
            .SumAsync(item => (decimal?)(item.Type == InvestmentMovementType.Contribution
                ? item.Amount : -item.Amount), cancellationToken) ?? 0;
        var investedBalance = profile.OpeningInvestedAmount + netInvestmentMovements;
        var monthlyMovementEntities = await dbContext.InvestmentMovements
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Date >= firstDay
                && item.Date <= lastDay && item.Date <= today)
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var monthlyInvestmentMovements = monthlyMovementEntities
            .Select(InvestmentMovementResponse.FromEntity)
            .ToList();
        var monthlySpendingPlan = await dbContext.MonthlySpendingPlans
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Year == selectedYear && item.Month == selectedMonth)
            .Select(item => (decimal?)item.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new DashboardResponse(
            selectedYear,
            selectedMonth,
            totalBalance,
            monthlyIncome,
            monthlyExpense,
            monthlyIncome - monthlyExpense,
            incomeReminderDue,
            monthlyIncomeRecorded,
            profile.EmploymentType,
            recentTransactions,
            profile.InitialCapital,
            profile.OpeningInvestedAmount,
            investedBalance,
            totalBalance - investedBalance,
            monthlySpendingPlan,
            monthlySpendingPlan - monthlyExpense,
            monthlyInvestmentMovements
                .Where(item => item.Type == InvestmentMovementType.Contribution)
                .Sum(item => item.Amount),
            monthlyInvestmentMovements
                .Where(item => item.Type == InvestmentMovementType.Withdrawal)
                .Sum(item => item.Amount),
            monthlyInvestmentMovements));
    }
}
