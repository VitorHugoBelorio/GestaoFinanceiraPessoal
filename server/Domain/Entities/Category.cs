using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }

    public User User { get; set; } = null!;
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = [];
}
