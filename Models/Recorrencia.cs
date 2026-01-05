public class Recorrencia
{
    public int Id { get; set; }

    // Ex: Mensal, Semanal, Anual
    public string Tipo { get; set; } = string.Empty;

    // A cada quantos períodos (ex: 1 mês, 2 meses)
    public int Intervalo { get; set; } = 1;

    // Data final da recorrência (opcional)
    public DateTime? DataFim { get; set; }

    // Controle interno
    public DateTime ProximaExecucao { get; set; }
}
