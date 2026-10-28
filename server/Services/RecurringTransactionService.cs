using System.Collections.Concurrent;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Services;

public class RecurringTransactionService(ApplicationDbContext dbContext)
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> UserLocks = new();

    public async Task MaterializeThroughAsync(
        int userId,
        DateOnly throughDate,
        CancellationToken cancellationToken = default)
    {
        await RunLockedAsync(userId, async () =>
        {
            await MaterializeInternalAsync(userId, throughDate, cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<T> RunLockedAsync<T>(
        int userId,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var userLock = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);

        try
        {
            return await action();
        }
        finally
        {
            userLock.Release();
        }
    }

    private async Task MaterializeInternalAsync(
        int userId,
        DateOnly throughDate,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var recurringTransactions = await dbContext.RecurringTransactions
            .Where(recurring => recurring.UserId == userId
                && recurring.IsActive
                && recurring.NextOccurrenceDate <= throughDate)
            .ToListAsync(cancellationToken);

        foreach (var recurring in recurringTransactions)
        {
            while (recurring.NextOccurrenceDate <= throughDate
                && (!recurring.EndDate.HasValue || recurring.NextOccurrenceDate <= recurring.EndDate.Value))
            {
                var occurrenceDate = recurring.NextOccurrenceDate;
                var alreadyExists = await dbContext.Transactions.AnyAsync(
                    transaction => transaction.RecurringTransactionId == recurring.Id
                        && transaction.Date == occurrenceDate,
                    cancellationToken);

                if (!alreadyExists)
                {
                    dbContext.Transactions.Add(new FinancialTransaction
                    {
                        UserId = userId,
                        Type = recurring.Type,
                        Description = recurring.Description,
                        Note = recurring.Note,
                        Amount = recurring.Amount,
                        Date = occurrenceDate,
                        Status = occurrenceDate <= today
                            ? TransactionStatus.Confirmed
                            : TransactionStatus.Planned,
                        CategoryId = recurring.CategoryId,
                        RecurringTransactionId = recurring.Id
                    });
                }

                recurring.NextOccurrenceDate = RecurrenceSchedule.GetNextMonthlyDate(
                    occurrenceDate,
                    recurring.DayOfMonth);
            }

            if (recurring.EndDate.HasValue && recurring.NextOccurrenceDate > recurring.EndDate.Value)
            {
                recurring.IsActive = false;
            }
        }

        var pendingOccurrences = await dbContext.Transactions
            .Where(transaction => transaction.UserId == userId
                && transaction.RecurringTransactionId != null
                && transaction.Status == TransactionStatus.Planned
                && transaction.Date <= today)
            .ToListAsync(cancellationToken);

        foreach (var occurrence in pendingOccurrences)
        {
            occurrence.Status = TransactionStatus.Confirmed;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

}
