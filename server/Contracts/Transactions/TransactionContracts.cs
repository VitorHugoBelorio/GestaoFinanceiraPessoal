using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Api.Domain;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Contracts.Transactions;

public class CreateTransactionRequest : IValidatableObject
{
    [Required, EnumDataType(typeof(TransactionType))]
    public TransactionType Type { get; set; }

    [Required, StringLength(180, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public bool IsMonthlyIncome { get; set; }

    [EnumDataType(typeof(TransactionStatus))]
    public TransactionStatus Status { get; set; } = TransactionStatus.Confirmed;

    public int? CategoryId { get; set; }

    public bool IsRecurring { get; set; }

    public bool IsInstallment { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true)]
    public decimal? TotalPurchaseAmount { get; set; }

    [Range(1, 31)]
    public int? RecurrenceDayOfMonth { get; set; }

    public DateOnly? RecurrenceEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date == default || Date.Year is < 2000 or > 2200)
        {
            yield return new ValidationResult("Informe uma data válida entre 2000 e 2200.", [nameof(Date)]);
        }

        if (RecurrenceEndDate.HasValue && RecurrenceEndDate.Value.Year > 2200)
        {
            yield return new ValidationResult("O mês final deve ser até 2200.", [nameof(RecurrenceEndDate)]);
        }

        if (IsMonthlyIncome && Type != TransactionType.Income)
        {
            yield return new ValidationResult("O recebimento mensal deve ser uma receita.", [nameof(IsMonthlyIncome)]);
        }

        if (IsMonthlyIncome && IsRecurring)
        {
            yield return new ValidationResult("O recebimento PJ deve ser registrado em cada mês, sem recorrência automática.", [nameof(IsRecurring)]);
        }

        if (IsMonthlyIncome && Status != TransactionStatus.Confirmed)
        {
            yield return new ValidationResult("Marque o recebimento mensal apenas quando ele estiver confirmado.", [nameof(Status)]);
        }

        if (IsMonthlyIncome && Date > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult("A data do recebimento não pode estar no futuro.", [nameof(Date)]);
        }

        if (IsInstallment)
        {
            if (!IsRecurring || Type != TransactionType.Expense)
            {
                yield return new ValidationResult("Parcelas são permitidas apenas para despesas mensais.", [nameof(IsInstallment)]);
            }

            if (!RecurrenceEndDate.HasValue)
            {
                yield return new ValidationResult("Informe quando termina o parcelamento.", [nameof(RecurrenceEndDate)]);
            }
            else if (RecurrenceEndDate.Value >= Date &&
                     RecurrenceSchedule.CountOccurrences(Date, RecurrenceEndDate.Value, Date.Day) < 2)
            {
                yield return new ValidationResult("O parcelamento precisa ter pelo menos duas parcelas mensais.", [nameof(RecurrenceEndDate)]);
            }
        }
        else if (TotalPurchaseAmount.HasValue)
        {
            yield return new ValidationResult("O valor total da compra só pode ser informado em despesas parceladas.", [nameof(TotalPurchaseAmount)]);
        }

        if (IsRecurring && !IsInstallment && !RecurrenceDayOfMonth.HasValue)
        {
            yield return new ValidationResult(
                "Informe o dia mensal da recorrência.",
                [nameof(RecurrenceDayOfMonth)]);
        }

        if (RecurrenceEndDate.HasValue && RecurrenceEndDate.Value < Date)
        {
            yield return new ValidationResult(
                "A data final da recorrência não pode ser anterior ao primeiro lançamento.",
                [nameof(RecurrenceEndDate)]);
        }
    }
}

public record TransactionResponse(
    int Id,
    TransactionType Type,
    string Description,
    string? Note,
    decimal Amount,
    DateOnly Date,
    TransactionStatus Status,
    int? CategoryId,
    string? CategoryName,
    bool IsRecurring,
    bool IsInstallment,
    int? InstallmentNumber,
    int? InstallmentCount,
    decimal? TotalPurchaseAmount,
    bool IsMonthlyIncome,
    DateOnly? RecurrenceStartDate,
    DateOnly? RecurrenceEndDate)
{
    public static TransactionResponse FromEntity(FinancialTransaction transaction)
    {
        var recurring = transaction.RecurringTransaction;
        var isInstallment = recurring?.IsInstallment == true;
        var installmentNumber = isInstallment
            ? (transaction.Date.Year - recurring!.StartDate.Year) * 12
                + transaction.Date.Month - recurring.StartDate.Month + 1
            : (int?)null;

        return new TransactionResponse(
            transaction.Id,
            transaction.Type,
            transaction.Description,
            transaction.Note,
            transaction.Amount,
            transaction.Date,
            transaction.Status,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.RecurringTransactionId.HasValue || recurring is not null,
            isInstallment,
            installmentNumber,
            isInstallment ? recurring!.InstallmentCount : null,
            isInstallment ? recurring!.TotalPurchaseAmount : null,
            transaction.IsMonthlyIncome,
            recurring?.StartDate,
            recurring?.EndDate);
    }
}

public class UpdateTransactionRequest : IValidatableObject
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public int? CategoryId { get; set; }

    public DateOnly? RecurrenceEndDate { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? TotalPurchaseAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date == default || Date.Year is < 2000 or > 2200)
        {
            yield return new ValidationResult("Informe uma data válida entre 2000 e 2200.", [nameof(Date)]);
        }

        if (RecurrenceEndDate.HasValue && RecurrenceEndDate.Value.Year > 2200)
        {
            yield return new ValidationResult("O mês final deve ser até 2200.", [nameof(RecurrenceEndDate)]);
        }
    }
}
