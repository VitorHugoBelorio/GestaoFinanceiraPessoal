namespace GestaoFinanceiraPessoal.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal Saldo => TotalReceitas - TotalDespesas;
        public List<DashboardDespesaViewModel> UltimasDespesas { get; set; } = new();
    }

    public class DashboardDespesaViewModel
    {
        public DateTime Data { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string CategoriaNome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public bool Recorrente { get; set; }
    }
}
