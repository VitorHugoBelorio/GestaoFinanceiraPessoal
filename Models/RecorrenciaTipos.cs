namespace GestaoFinanceiraPessoal.Models
{
    public static class RecorrenciaTipos
    {
        public const string Mensal = "Mensal";
        public const string Semanal = "Semanal";
        public const string Anual = "Anual";

        public static readonly string[] Todos =
        [
            Mensal,
            Semanal,
            Anual
        ];

        public static string Normalizar(string? tipo)
        {
            return tipo switch
            {
                Semanal => Semanal,
                Anual => Anual,
                _ => Mensal
            };
        }
    }
}
