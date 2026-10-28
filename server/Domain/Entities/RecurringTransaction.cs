using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class RecurringTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsInstallment { get; set; }
    public int? InstallmentCount { get; set; }
    public decimal? TotalPurchaseAmount { get; set; }
    public DateOnly NextOccurrenceDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CategoryId { get; set; }

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
}
