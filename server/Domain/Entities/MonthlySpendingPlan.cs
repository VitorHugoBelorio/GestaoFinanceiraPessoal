namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class MonthlySpendingPlan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }

    public User User { get; set; } = null!;
}
