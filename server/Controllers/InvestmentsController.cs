using GestaoFinanceiraPessoal.Api.Contracts.Investments;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Domain.Enums;
using GestaoFinanceiraPessoal.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/investments")]
public class InvestmentsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPut("opening-amount")]
    public async Task<IActionResult> SetOpeningAmount(
        SetOpeningInvestedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await dbContext.FinancialProfiles.FirstOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken);

        if (profile is null) return Conflict();

        if (request.Amount > profile.InitialCapital)
        {
            ModelState.AddModelError(nameof(request.Amount), "O valor inicial aplicado não pode superar o capital informado no cadastro.");
            return ValidationProblem(ModelState);
        }

        var netMovements = await dbContext.InvestmentMovements
            .Where(item => item.UserId == userId)
            .SumAsync(item => (decimal?)(item.Type == InvestmentMovementType.Contribution
                ? item.Amount : -item.Amount), cancellationToken) ?? 0;

        if (request.Amount + netMovements < 0)
        {
            ModelState.AddModelError(nameof(request.Amount), "O valor inicial aplicado não pode deixar o total investido negativo.");
            return ValidationProblem(ModelState);
        }

        profile.OpeningInvestedAmount = request.Amount;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("monthly-spending-plan")]
    public async Task<IActionResult> SetMonthlySpendingPlan(
        SetMonthlySpendingPlanRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var plan = await dbContext.MonthlySpendingPlans.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Year == request.Year && item.Month == request.Month,
            cancellationToken);

        if (!request.Amount.HasValue)
        {
            if (plan is not null) dbContext.MonthlySpendingPlans.Remove(plan);
        }
        else if (plan is null)
        {
            dbContext.MonthlySpendingPlans.Add(new MonthlySpendingPlan
            {
                UserId = userId,
                Year = request.Year,
                Month = request.Month,
                Amount = request.Amount.Value
            });
        }
        else
        {
            plan.Amount = request.Amount.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("movements")]
    public async Task<ActionResult<InvestmentMovementResponse>> CreateMovement(
        CreateInvestmentMovementRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await dbContext.FinancialProfiles.AsNoTracking().FirstOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken);

        if (profile is null) return Conflict();

        var netMovements = await dbContext.InvestmentMovements
            .Where(item => item.UserId == userId)
            .SumAsync(item => (decimal?)(item.Type == InvestmentMovementType.Contribution
                ? item.Amount : -item.Amount), cancellationToken) ?? 0;
        var investedBalance = profile.OpeningInvestedAmount + netMovements;

        if (request.Type == InvestmentMovementType.Withdrawal && investedBalance < request.Amount)
        {
            ModelState.AddModelError(nameof(request.Amount), "O resgate supera o valor aplicado registrado.");
            return ValidationProblem(ModelState);
        }

        if (request.Type == InvestmentMovementType.Contribution)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var confirmedIncome = await dbContext.Transactions
                .Where(item => item.UserId == userId && item.Type == TransactionType.Income
                    && item.Status == TransactionStatus.Confirmed && item.Date <= today)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
            var confirmedExpense = await dbContext.Transactions
                .Where(item => item.UserId == userId && item.Type == TransactionType.Expense
                    && item.Status == TransactionStatus.Confirmed && item.Date <= today)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;

            if (request.Amount > profile.InitialCapital + confirmedIncome - confirmedExpense - investedBalance)
            {
                ModelState.AddModelError(nameof(request.Amount), "O aporte supera o valor disponível registrado.");
                return ValidationProblem(ModelState);
            }
        }

        var movement = new InvestmentMovement
        {
            UserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        dbContext.InvestmentMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, InvestmentMovementResponse.FromEntity(movement));
    }

    [HttpDelete("movements/{id:int}")]
    public async Task<IActionResult> DeleteMovement(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var movement = await dbContext.InvestmentMovements.FirstOrDefaultAsync(
            item => item.Id == id && item.UserId == userId,
            cancellationToken);

        if (movement is null) return NotFound();

        if (movement.Type == InvestmentMovementType.Contribution)
        {
            var openingAmount = await dbContext.FinancialProfiles
                .Where(item => item.UserId == userId)
                .Select(item => item.OpeningInvestedAmount)
                .SingleAsync(cancellationToken);
            var netMovements = await dbContext.InvestmentMovements
                .Where(item => item.UserId == userId)
                .SumAsync(item => (decimal?)(item.Type == InvestmentMovementType.Contribution
                    ? item.Amount : -item.Amount), cancellationToken) ?? 0;

            if (openingAmount + netMovements - movement.Amount < 0)
            {
                ModelState.AddModelError(nameof(id), "Este aporte não pode ser excluído enquanto houver resgates que dependem dele.");
                return ValidationProblem(ModelState);
            }
        }

        dbContext.InvestmentMovements.Remove(movement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
