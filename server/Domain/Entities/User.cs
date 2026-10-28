namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public FinancialProfile? FinancialProfile { get; set; }
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = [];
    public ICollection<InvestmentMovement> InvestmentMovements { get; set; } = [];
    public ICollection<MonthlySpendingPlan> MonthlySpendingPlans { get; set; } = [];
}
