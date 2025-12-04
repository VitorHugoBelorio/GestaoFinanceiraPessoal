using GestaoFinanceiraPessoal.Enuns;
using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Receita
    {
        public int Id { get; set; }

        [Required]
        public string Descricao { get; set; }

        [Required]
        public decimal Valor { get; set; }

        [Required]
        public int Mes { get; set; }

        [Required]
        public int Ano { get; set; }

        [Required]
        public ETiposReceita Tipo { get; set; }
    }
}
