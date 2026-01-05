using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Receita
    {
        public int Id { get; set; }

        public string Descricao { get; set; }
        public decimal Valor { get; set; }

        public DateTime Data { get; set; }

        public int? RecorrenciaId { get; set; }
        public Recorrencia? Recorrencia { get; set; }
    }

}
