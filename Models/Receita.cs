using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Receita
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Descrição é obrigatória")]
        public string Descricao { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Data é obrigatória")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        public int? RecorrenciaId { get; set; }
        public Recorrencia? Recorrencia { get; set; }

        // Foreign key to identify owner (User)
        public int UserId { get; set; }
    }

}
