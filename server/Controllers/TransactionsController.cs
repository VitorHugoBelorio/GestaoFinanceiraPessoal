using GestaoFinanceiraPessoal.Api.Contracts.Transactions;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Extensions;
using GestaoFinanceiraPessoal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController(
    ApplicationDbContext dbContext,
    RecurringTransactionService recurringTransactionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TransactionResponse>>> List(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var referenceDate = DateOnly.FromDateTime(DateTime.Today);
        var selectedYear = year ?? referenceDate.Year;
        var selectedMonth = month ?? referenceDate.Month;

        if (selectedYear is < 2000 or > 2200 || selectedMonth is < 1 or > 12)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "O período informado é inválido.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userId = User.GetRequiredUserId();

        var firstDay = new DateOnly(selectedYear, selectedMonth, 1);
        var lastDay = new DateOnly(
            selectedYear,
            selectedMonth,
            DateTime.DaysInMonth(selectedYear, selectedMonth));

        await recurringTransactionService.MaterializeThroughAsync(
            userId,
            lastDay,
            cancellationToken);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Category)
            .Include(transaction => transaction.RecurringTransaction)
            .Where(transaction => transaction.UserId == userId
                && transaction.Date >= firstDay
                && transaction.Date <= lastDay)
            .OrderByDescending(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Description)
            .ToListAsync(cancellationToken);

        return Ok(transactions.Select(TransactionResponse.FromEntity).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.RecurringTransaction)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        return transaction is null ? NotFound() : Ok(TransactionResponse.FromEntity(transaction));
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        if (request.IsMonthlyIncome && !await dbContext.FinancialProfiles.AnyAsync(
            profile => profile.UserId == userId && profile.EmploymentType == GestaoFinanceiraPessoal.Api.Domain.Enums.EmploymentType.Pj,
            cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IsMonthlyIncome), "Somente contas PJ podem identificar um recebimento mensal.");
            return ValidationProblem(ModelState);
        }

        if (request.CategoryId.HasValue)
        {
            var categoryIsValid = await dbContext.Categories.AnyAsync(
                category => category.Id == request.CategoryId.Value
                    && category.UserId == userId
                    && category.Type == request.Type,
                cancellationToken);

            if (!categoryIsValid)
            {
                ModelState.AddModelError(
                    nameof(request.CategoryId),
                    "A categoria informada não pertence a este usuário ou não corresponde ao tipo do lançamento.");
                return ValidationProblem(ModelState);
            }
        }

        RecurringTransaction? recurring = null;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (request.IsRecurring)
        {
            var dayOfMonth = request.IsInstallment
                ? request.Date.Day
                : request.RecurrenceDayOfMonth!.Value;
            recurring = new RecurringTransaction
            {
                UserId = userId,
                Type = request.Type,
                Description = request.Description.Trim(),
                Note = note,
                Amount = request.Amount,
                DayOfMonth = dayOfMonth,
                StartDate = request.Date,
                EndDate = request.RecurrenceEndDate,
                IsInstallment = request.IsInstallment,
                InstallmentCount = request.IsInstallment
                    ? RecurrenceSchedule.CountOccurrences(request.Date, request.RecurrenceEndDate!.Value, dayOfMonth)
                    : null,
                TotalPurchaseAmount = request.IsInstallment ? request.TotalPurchaseAmount : null,
                NextOccurrenceDate = RecurrenceSchedule.GetNextMonthlyDate(
                    request.Date,
                    dayOfMonth),
                CategoryId = request.CategoryId
            };
        }

        var transaction = new FinancialTransaction
        {
            UserId = userId,
            Type = request.Type,
            Description = request.Description.Trim(),
            Note = note,
            Amount = request.Amount,
            Date = request.Date,
            Status = request.IsRecurring && request.Date > DateOnly.FromDateTime(DateTime.Today)
                ? GestaoFinanceiraPessoal.Api.Domain.Enums.TransactionStatus.Planned
                : request.Status,
            IsMonthlyIncome = request.IsMonthlyIncome,
            CategoryId = request.CategoryId,
            RecurringTransaction = recurring
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(List),
            new { year = transaction.Date.Year, month = transaction.Date.Month },
            TransactionResponse.FromEntity(transaction));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        return await recurringTransactionService.RunLockedAsync<IActionResult>(userId, async () =>
        {
            var transaction = await dbContext.Transactions
                .Include(item => item.RecurringTransaction)
                .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
            if (transaction is null) return NotFound();

            if (request.CategoryId.HasValue)
            {
                var categoryIsValid = await dbContext.Categories.AnyAsync(
                    category => category.Id == request.CategoryId.Value
                        && category.UserId == userId
                        && category.Type == transaction.Type,
                    cancellationToken);
                if (!categoryIsValid)
                {
                    ModelState.AddModelError(nameof(request.CategoryId), "A etiqueta não pertence à sua conta ou ao tipo deste lançamento.");
                    return ValidationProblem(ModelState);
                }
            }

            var recurring = transaction.RecurringTransaction;
            if (recurring is null)
            {
                if (request.RecurrenceEndDate.HasValue || request.TotalPurchaseAmount.HasValue)
                {
                    ModelState.AddModelError(nameof(request.RecurrenceEndDate), "Este lançamento não é recorrente.");
                    return ValidationProblem(ModelState);
                }
                if (transaction.IsMonthlyIncome && request.Date > DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError(nameof(request.Date), "A data do recebimento não pode estar no futuro.");
                    return ValidationProblem(ModelState);
                }

                transaction.Description = request.Description.Trim();
                transaction.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
                transaction.Amount = request.Amount;
                transaction.Date = request.Date;
                transaction.CategoryId = request.CategoryId;
            }
            else
            {
                if (request.Date != transaction.Date)
                {
                    ModelState.AddModelError(nameof(request.Date), "A data de uma ocorrência não pode ser alterada. Edite a série sem mudar o dia inicial.");
                    return ValidationProblem(ModelState);
                }
                if (request.RecurrenceEndDate.HasValue && request.RecurrenceEndDate.Value < recurring.StartDate)
                {
                    ModelState.AddModelError(nameof(request.RecurrenceEndDate), "O mês final não pode ser anterior ao primeiro lançamento.");
                    return ValidationProblem(ModelState);
                }
                if (recurring.IsInstallment && (!request.RecurrenceEndDate.HasValue
                    || RecurrenceSchedule.CountOccurrences(recurring.StartDate, request.RecurrenceEndDate.Value, recurring.DayOfMonth) < 2))
                {
                    ModelState.AddModelError(nameof(request.RecurrenceEndDate), "Uma despesa com prazo definido precisa ter pelo menos dois meses.");
                    return ValidationProblem(ModelState);
                }
                if (!recurring.IsInstallment && request.TotalPurchaseAmount.HasValue)
                {
                    ModelState.AddModelError(nameof(request.TotalPurchaseAmount), "O valor total só pode ser informado para despesas com prazo definido.");
                    return ValidationProblem(ModelState);
                }

                var occurrences = await dbContext.Transactions
                    .Where(item => item.UserId == userId && item.RecurringTransactionId == recurring.Id)
                    .OrderBy(item => item.Date)
                    .ToListAsync(cancellationToken);

                recurring.Description = request.Description.Trim();
                recurring.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
                recurring.Amount = request.Amount;
                recurring.CategoryId = request.CategoryId;
                recurring.EndDate = request.RecurrenceEndDate;
                recurring.InstallmentCount = recurring.IsInstallment
                    ? RecurrenceSchedule.CountOccurrences(recurring.StartDate, request.RecurrenceEndDate!.Value, recurring.DayOfMonth)
                    : null;
                recurring.TotalPurchaseAmount = recurring.IsInstallment ? request.TotalPurchaseAmount : null;

                foreach (var occurrence in occurrences)
                {
                    if (recurring.EndDate.HasValue && occurrence.Date > recurring.EndDate.Value)
                    {
                        dbContext.Transactions.Remove(occurrence);
                        continue;
                    }
                    occurrence.Description = recurring.Description;
                    occurrence.Note = recurring.Note;
                    occurrence.Amount = recurring.Amount;
                    occurrence.CategoryId = recurring.CategoryId;
                }

                var lastDate = occurrences
                    .Where(item => !recurring.EndDate.HasValue || item.Date <= recurring.EndDate.Value)
                    .Select(item => item.Date)
                    .LastOrDefault();
                recurring.NextOccurrenceDate = RecurrenceSchedule.GetNextMonthlyDate(
                    lastDate == default ? recurring.StartDate : lastDate,
                    recurring.DayOfMonth);
                recurring.IsActive = !recurring.EndDate.HasValue || recurring.NextOccurrenceDate <= recurring.EndDate.Value;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        return await recurringTransactionService.RunLockedAsync<IActionResult>(userId, async () =>
        {
            var transaction = await dbContext.Transactions
                .Include(item => item.RecurringTransaction)
                .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

            if (transaction is null) return NotFound();

            if (transaction.RecurringTransaction is { } recurring)
            {
                var occurrences = await dbContext.Transactions
                    .Where(item => item.UserId == userId && item.RecurringTransactionId == recurring.Id)
                    .ToListAsync(cancellationToken);
                dbContext.Transactions.RemoveRange(occurrences);
                dbContext.RecurringTransactions.Remove(recurring);
            }
            else
            {
                dbContext.Transactions.Remove(transaction);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }, cancellationToken);
    }

}
