using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Receita
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Descricao e obrigatoria")]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Data e obrigatoria")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        public bool EhModeloRecorrente { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DataReferenciaRecorrencia { get; set; }

        public bool OcorrenciaCancelada { get; set; }

        public int? RecorrenciaId { get; set; }
        public Recorrencia? Recorrencia { get; set; }

        public int UserId { get; set; }
    }
}
