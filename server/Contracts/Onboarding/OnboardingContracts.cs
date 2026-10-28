using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Contracts.Onboarding;

public class CompleteOnboardingRequest : IValidatableObject
{
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Informe um capital válido dentro do limite permitido.")]
    public decimal InitialCapital { get; set; }

    [Required(ErrorMessage = "Escolha o perfil profissional."),
     EnumDataType(typeof(EmploymentType), ErrorMessage = "Escolha CLT ou PJ.")]
    public EmploymentType EmploymentType { get; set; }

    [Required(ErrorMessage = "Escolha como registrar a renda."),
     EnumDataType(typeof(IncomeSetupMode), ErrorMessage = "A forma de registro da renda é inválida.")]
    public IncomeSetupMode IncomeSetupMode { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Informe uma renda mensal maior que zero e dentro do limite permitido.")]
    public decimal? FixedMonthlyIncome { get; set; }

    [Range(1, 31, ErrorMessage = "Escolha um dia de recebimento entre 1 e 31.")]
    public int? IncomeDayOfMonth { get; set; }
    [Range(1, 28, ErrorMessage = "Escolha um dia do lembrete entre 1 e 28.")]
    public int? IncomeReminderDay { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((EmploymentType == EmploymentType.Pj && IncomeSetupMode == IncomeSetupMode.FixedMonthly) ||
            (EmploymentType == EmploymentType.Clt && IncomeSetupMode == IncomeSetupMode.MonthlyReminder))
        {
            yield return new ValidationResult("Essa forma de registro não está disponível para o perfil selecionado.", [nameof(IncomeSetupMode)]);
        }

        if (IncomeSetupMode == IncomeSetupMode.MonthlyReminder && !IncomeReminderDay.HasValue)
        {
            yield return new ValidationResult("Informe um dia do mês entre 1 e 28 para o lembrete.", [nameof(IncomeReminderDay)]);
        }
        if (IncomeSetupMode == IncomeSetupMode.FixedMonthly)
        {
            if (!FixedMonthlyIncome.HasValue)
            {
                yield return new ValidationResult(
                    "Informe o valor da renda fixa mensal.",
                    [nameof(FixedMonthlyIncome)]);
            }

            if (!IncomeDayOfMonth.HasValue)
            {
                yield return new ValidationResult(
                    "Informe o dia de recebimento da renda fixa.",
                    [nameof(IncomeDayOfMonth)]);
            }
        }

    }
}

public record OnboardingStatusResponse(
    bool Completed,
    decimal? InitialCapital,
    EmploymentType? EmploymentType,
    IncomeSetupMode? IncomeSetupMode,
    int? MonthlyIncomeReminderDay);

public class UpdateIncomeReminderRequest
{
    [Required, Range(1, 28)]
    public int? DayOfMonth { get; set; }
}
