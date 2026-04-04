using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Models;

namespace GestaoFinanceiraPessoal.Models.ViewModels
{
    public class ReceitaIndexViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Total { get; set; }
        public bool ExibirCanceladas { get; set; }
        public List<ReceitaListaItemViewModel> Itens { get; set; } = new();
    }

    public class ReceitaListaItemViewModel
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime DataExibicao { get; set; }
        public bool Recorrente { get; set; }
        public bool OcorrenciaCancelada { get; set; }
    }

    public class ReceitaFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Descricao e obrigatoria")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Valor e obrigatorio")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Informe um valor maior que zero")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Data e obrigatoria")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; } = DateTime.Today;

        public bool Recorrente { get; set; }

        public string RecorrenciaTipo { get; set; } = RecorrenciaTipos.Mensal;

        [Range(1, 120, ErrorMessage = "O intervalo deve estar entre 1 e 120")]
        public int RecorrenciaIntervalo { get; set; } = 1;

        [DataType(DataType.Date)]
        public DateTime? RecorrenciaDataFim { get; set; }

        public bool PermiteEditarConfiguracaoRecorrencia { get; set; } = true;

        public bool ExibirAvisoEdicaoCompetencia { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Recorrente)
            {
                yield break;
            }

            RecorrenciaTipo = RecorrenciaTipos.Normalizar(RecorrenciaTipo);

            if (RecorrenciaDataFim.HasValue && RecorrenciaDataFim.Value.Date < Data.Date)
            {
                yield return new ValidationResult(
                    "A data final da recorrencia deve ser maior ou igual a data inicial.",
                    [nameof(RecorrenciaDataFim)]);
            }
        }
    }
}
