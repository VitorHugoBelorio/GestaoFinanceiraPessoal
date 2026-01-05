using GestaoFinanceiraPessoal.Enums;

namespace GestaoFinanceiraPessoal.Models
{
    public class Recorrencia
    {
        public int Id { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public bool Ativa { get; set; } = true;

        public ETipoRecorrencia Tipo { get; set; }
    }

}
