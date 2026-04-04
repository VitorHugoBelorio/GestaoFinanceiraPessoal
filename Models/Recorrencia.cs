namespace GestaoFinanceiraPessoal.Models
{
    public class Recorrencia
    {
        public int Id { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public int Intervalo { get; set; } = 1;

        public int DiaReferencia { get; set; }

        public int MesReferencia { get; set; }

        public DateTime? DataFim { get; set; }

        public DateTime ProximaExecucao { get; set; }
    }
}
