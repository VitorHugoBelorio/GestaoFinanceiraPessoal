using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Contracts.Investments;

public class SetOpeningInvestedRequest
{
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }
}

public class SetMonthlySpendingPlanRequest
{
    [Range(2000, 2200)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    public bool CarryOverUnused { get; set; }
}

public class CreateInvestmentMovementRequest : IValidatableObject
{
    [Required, EnumDataType(typeof(InvestmentMovementType))]
    public InvestmentMovementType Type { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsFromMonthlyIncome { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == InvestmentMovementType.Withdrawal && IsFromMonthlyIncome)
        {
            yield return new ValidationResult(
                "A origem na renda mensal só pode ser usada em aportes.",
                [nameof(IsFromMonthlyIncome)]);
        }

        if (Date.Year < 2000 || Date > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Informe uma data válida que não esteja no futuro.",
                [nameof(Date)]);
        }
    }
}

public record InvestmentMovementResponse(
    int Id,
    InvestmentMovementType Type,
    decimal Amount,
    DateOnly Date,
    string? Note,
    bool IsFromMonthlyIncome)
{
    public static InvestmentMovementResponse FromEntity(InvestmentMovement movement) =>
        new(movement.Id, movement.Type, movement.Amount, movement.Date, movement.Note, movement.IsFromMonthlyIncome);
}

public class SaveInvestmentAllocationRequest
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }
}

public record InvestmentAllocationResponse(int Id, string Name, decimal Amount)
{
    public static InvestmentAllocationResponse FromEntity(InvestmentAllocation allocation) =>
        new(allocation.Id, allocation.Name, allocation.Amount);
}
