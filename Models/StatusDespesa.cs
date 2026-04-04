namespace GestaoFinanceiraPessoal.Models
{
    public static class StatusDespesa
    {
        public const string Pendente = "Pendente";
        public const string Pago = "Pago";

        public static readonly string[] Todos =
        [
            Pendente,
            Pago
        ];

        public static string Normalizar(string? status)
        {
            return status == Pago ? Pago : Pendente;
        }
    }
}
