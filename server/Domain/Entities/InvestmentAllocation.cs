namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class InvestmentAllocation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public User User { get; set; } = null!;
}
