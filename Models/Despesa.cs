using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Despesa
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Descricao e obrigatoria")]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Data e obrigatoria")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        public bool EhModeloRecorrente { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DataReferenciaRecorrencia { get; set; }

        public bool OcorrenciaCancelada { get; set; }

        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        [Required(ErrorMessage = "Status e obrigatorio")]
        public string Status { get; set; } = StatusDespesa.Pendente;

        public int? RecorrenciaId { get; set; }
        public Recorrencia? Recorrencia { get; set; }

        public int UserId { get; set; }
    }
}
