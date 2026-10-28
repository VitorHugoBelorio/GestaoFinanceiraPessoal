using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Contracts.Categories;

public class CreateCategoryRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(TransactionType))]
    public TransactionType Type { get; set; } = TransactionType.Expense;
}

public record CategoryResponse(int Id, string Name, TransactionType Type);
