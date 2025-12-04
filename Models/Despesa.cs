using GestaoFinanceiraPessoal.Enuns;
using System;
using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Despesa
    {
        public int Id { get; set; }

        [Required]
        public decimal Valor { get; set; }

        [Required]
        public string Descricao { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public ETiposDespesas Tipo { get; set; }

        // Relacionamento
        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; }
    }
}
