using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class FinancialTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Confirmed;
    public bool IsMonthlyIncome { get; set; }
    public int? CategoryId { get; set; }
    public int? RecurringTransactionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }
}
