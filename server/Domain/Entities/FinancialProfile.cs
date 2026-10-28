using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Domain.Entities;

public class FinancialProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal OpeningInvestedAmount { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public IncomeSetupMode IncomeSetupMode { get; set; }
    public int? MonthlyIncomeReminderDay { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
