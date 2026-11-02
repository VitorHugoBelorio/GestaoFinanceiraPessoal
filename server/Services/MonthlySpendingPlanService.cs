using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Services;

public record MonthlySpendingPlanSnapshot(
    decimal? BaseAmount,
    decimal CarryIn,
    decimal? EffectiveAmount,
    decimal? Remaining,
    bool CarryOverUnused);

public class MonthlySpendingPlanService(ApplicationDbContext dbContext)
{
    public async Task<MonthlySpendingPlanSnapshot> GetSnapshotAsync(
        int userId,
        int year,
        int month,
        decimal monthlyExpense,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var selectedMonth = new DateOnly(year, month, 1);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var plans = await dbContext.MonthlySpendingPlans
            .AsNoTracking()
            .Where(plan => plan.UserId == userId
                && (plan.Year < year || (plan.Year == year && plan.Month <= month)))
            .ToListAsync(cancellationToken);
        var plansByMonth = plans.ToDictionary(
            plan => new DateOnly(plan.Year, plan.Month, 1));

        // Only a consecutive chain of months opting into carry-over can affect this month.
        var firstRelevantMonth = selectedMonth;
        while (firstRelevantMonth.AddMonths(-1) < currentMonth
            && plansByMonth.TryGetValue(firstRelevantMonth.AddMonths(-1), out var previousPlan)
            && previousPlan.CarryOverUnused)
        {
            firstRelevantMonth = firstRelevantMonth.AddMonths(-1);
        }

        var expenseByMonth = new Dictionary<DateOnly, decimal>();
        if (firstRelevantMonth < selectedMonth)
        {
            var priorExpenses = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId
                    && transaction.Type == TransactionType.Expense
                    && transaction.Status == TransactionStatus.Confirmed
                    && transaction.Date >= firstRelevantMonth
                    && transaction.Date < selectedMonth)
                .Select(transaction => new { transaction.Date, transaction.Amount })
                .ToListAsync(cancellationToken);

            expenseByMonth = priorExpenses
                .GroupBy(transaction => new DateOnly(transaction.Date.Year, transaction.Date.Month, 1))
                .ToDictionary(group => group.Key, group => group.Sum(transaction => transaction.Amount));
        }

        decimal previousEffectiveAmount = 0;
        DateOnly? processedMonth = null;
        for (var period = firstRelevantMonth; period <= selectedMonth; period = period.AddMonths(1))
        {
            var priorPeriod = period.AddMonths(-1);
            var carryIn = processedMonth == priorPeriod
                && priorPeriod < currentMonth
                && plansByMonth.TryGetValue(priorPeriod, out var priorPlan)
                && priorPlan.CarryOverUnused
                    ? Math.Max(0, previousEffectiveAmount - expenseByMonth.GetValueOrDefault(priorPeriod))
                    : 0;

            if (!plansByMonth.TryGetValue(period, out var plan))
            {
                return new MonthlySpendingPlanSnapshot(null, carryIn, null, null, false);
            }

            var effectiveAmount = plan.Amount + carryIn;
            if (period == selectedMonth)
            {
                return new MonthlySpendingPlanSnapshot(
                    plan.Amount,
                    carryIn,
                    effectiveAmount,
                    effectiveAmount - monthlyExpense,
                    plan.CarryOverUnused);
            }

            previousEffectiveAmount = effectiveAmount;
            processedMonth = period;
        }

        throw new InvalidOperationException("Não foi possível calcular o planejamento mensal.");
    }
}
