namespace GestaoFinanceiraPessoal.Models.ViewModels
{
    public class DespesaIndexViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Total { get; set; }
        public List<DespesaListaItemViewModel> Itens { get; set; } = new();
    }

    public class DespesaListaItemViewModel
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string CategoriaNome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime DataExibicao { get; set; }
        public bool Recorrente { get; set; }
    }
}
