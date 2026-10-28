using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class InvestmentMovement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public InvestmentMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
