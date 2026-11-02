using System.Data;
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
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

        var allocated = await dbContext.InvestmentAllocations
            .Where(item => item.UserId == userId)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        if (request.Amount + netMovements < allocated)
        {
            ModelState.AddModelError(nameof(request.Amount), "Ajuste primeiro os valores dos grupos da carteira para que não superem o total aplicado.");
            return ValidationProblem(ModelState);
        }

        profile.OpeningInvestedAmount = request.Amount;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
                Amount = request.Amount.Value,
                CarryOverUnused = request.CarryOverUnused
            });
        }
        else
        {
            plan.Amount = request.Amount.Value;
            plan.CarryOverUnused = request.CarryOverUnused;
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
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

        if (request.Type == InvestmentMovementType.Withdrawal)
        {
            var allocated = await dbContext.InvestmentAllocations
                .Where(item => item.UserId == userId)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
            if (investedBalance - request.Amount < allocated)
            {
                ModelState.AddModelError(nameof(request.Amount), "Ajuste primeiro os grupos da carteira para liberar o valor que deseja resgatar.");
                return ValidationProblem(ModelState);
            }
        }

        if (request.Type == InvestmentMovementType.Contribution)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (request.IsFromMonthlyIncome)
            {
                var firstDay = new DateOnly(request.Date.Year, request.Date.Month, 1);
                var receivedThisMonth = await dbContext.Transactions
                    .Where(item => item.UserId == userId && item.Type == TransactionType.Income
                        && item.Status == TransactionStatus.Confirmed
                        && item.Date >= firstDay && item.Date <= request.Date)
                    .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
                var alreadyAllocated = await dbContext.InvestmentMovements
                    .Where(item => item.UserId == userId
                        && item.Type == InvestmentMovementType.Contribution
                        && item.IsFromMonthlyIncome
                        && item.Date >= firstDay && item.Date < firstDay.AddMonths(1))
                    .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;

                if (request.Amount > receivedThisMonth - alreadyAllocated)
                {
                    ModelState.AddModelError(nameof(request.Amount),
                        "O valor informado supera a renda recebida neste mês ainda não atribuída a aportes. Registre o recebimento ou ajuste o valor.");
                    return ValidationProblem(ModelState);
                }
            }

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
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            IsFromMonthlyIncome = request.Type == InvestmentMovementType.Contribution && request.IsFromMonthlyIncome
        };

        dbContext.InvestmentMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, InvestmentMovementResponse.FromEntity(movement));
    }

    [HttpDelete("movements/{id:int}")]
    public async Task<IActionResult> DeleteMovement(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
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

            var allocated = await dbContext.InvestmentAllocations
                .Where(item => item.UserId == userId)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
            if (openingAmount + netMovements - movement.Amount < allocated)
            {
                ModelState.AddModelError(nameof(id), "Ajuste primeiro os grupos da carteira antes de remover este aporte.");
                return ValidationProblem(ModelState);
            }
        }

        dbContext.InvestmentMovements.Remove(movement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<IReadOnlyCollection<InvestmentAllocationResponse>>> ListAllocations(
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var allocations = await dbContext.InvestmentAllocations
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return Ok(allocations.Select(InvestmentAllocationResponse.FromEntity).ToList());
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<InvestmentAllocationResponse>> CreateAllocation(
        SaveInvestmentAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(request.Name), "Dê um nome ao grupo da carteira.");
            return ValidationProblem(ModelState);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (await dbContext.InvestmentAllocations.AnyAsync(
            item => item.UserId == userId && item.Name == name, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "Você já tem um grupo da carteira com esse nome.");
            return ValidationProblem(ModelState);
        }

        var investedBalance = await GetInvestedBalanceAsync(userId, cancellationToken);
        if (!investedBalance.HasValue) return Conflict();
        var allocated = await dbContext.InvestmentAllocations
            .Where(item => item.UserId == userId)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        if (request.Amount > investedBalance.Value - allocated)
        {
            ModelState.AddModelError(nameof(request.Amount), "O valor do grupo supera a parte aplicada ainda não classificada.");
            return ValidationProblem(ModelState);
        }

        var allocation = new InvestmentAllocation { UserId = userId, Name = name, Amount = request.Amount };
        dbContext.InvestmentAllocations.Add(allocation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, InvestmentAllocationResponse.FromEntity(allocation));
    }

    [HttpPut("allocations/{id:int}")]
    public async Task<IActionResult> UpdateAllocation(
        int id,
        SaveInvestmentAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(request.Name), "Dê um nome ao grupo da carteira.");
            return ValidationProblem(ModelState);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var allocation = await dbContext.InvestmentAllocations.FirstOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (allocation is null) return NotFound();
        if (await dbContext.InvestmentAllocations.AnyAsync(
            item => item.UserId == userId && item.Id != id && item.Name == name, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "Você já tem um grupo da carteira com esse nome.");
            return ValidationProblem(ModelState);
        }

        var investedBalance = await GetInvestedBalanceAsync(userId, cancellationToken);
        if (!investedBalance.HasValue) return Conflict();
        var otherAllocations = await dbContext.InvestmentAllocations
            .Where(item => item.UserId == userId && item.Id != id)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        if (request.Amount > investedBalance.Value - otherAllocations)
        {
            ModelState.AddModelError(nameof(request.Amount), "O valor do grupo supera a parte aplicada disponível para classificação.");
            return ValidationProblem(ModelState);
        }

        allocation.Name = name;
        allocation.Amount = request.Amount;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("allocations/{id:int}")]
    public async Task<IActionResult> DeleteAllocation(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var allocation = await dbContext.InvestmentAllocations.FirstOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (allocation is null) return NotFound();

        dbContext.InvestmentAllocations.Remove(allocation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<decimal?> GetInvestedBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        var openingAmount = await dbContext.FinancialProfiles
            .Where(item => item.UserId == userId)
            .Select(item => (decimal?)item.OpeningInvestedAmount)
            .SingleOrDefaultAsync(cancellationToken);
        if (!openingAmount.HasValue) return null;

        var netMovements = await dbContext.InvestmentMovements
            .Where(item => item.UserId == userId)
            .SumAsync(item => (decimal?)(item.Type == InvestmentMovementType.Contribution
                ? item.Amount : -item.Amount), cancellationToken) ?? 0;
        return openingAmount.Value + netMovements;
    }
}
