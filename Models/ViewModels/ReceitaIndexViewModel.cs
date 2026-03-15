namespace GestaoFinanceiraPessoal.Models.ViewModels
{
    public class ReceitaIndexViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Total { get; set; }
        public List<ReceitaListaItemViewModel> Itens { get; set; } = new();
    }

    public class ReceitaListaItemViewModel
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime DataExibicao { get; set; }
        public bool Recorrente { get; set; }
    }
}
