using GestaoFinanceiraPessoal.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceiraPessoal.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Nome { get; set; } = string.Empty;

        // Owned despesas
        public List<Despesa> Despesas { get; set; } = new List<Despesa>();

        // Foreign key to identify owner (User)
        public int UserId { get; set; }
    }
}
