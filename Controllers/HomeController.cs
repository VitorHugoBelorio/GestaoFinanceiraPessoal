using GestaoFinanceiraPessoal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{

    [Authorize]
    [Route("")]
    [Route("dashboard")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? ano, int? mes)
        {
            var anoAtual = ano ?? DateTime.Now.Year;
            var mesAtual = mes ?? DateTime.Now.Month;

            var receitasMes = await _context.Receitas
                .Where(r => r.Data.Year == anoAtual && r.Data.Month == mesAtual)
                .SumAsync(r => (decimal?)r.Valor) ?? 0;

            var despesasMes = await _context.Despesas
                .Where(d => d.Data.Year == anoAtual && d.Data.Month == mesAtual)
                .SumAsync(d => (decimal?)d.Valor) ?? 0;

            var saldo = receitasMes - despesasMes;

            var ultimasMovimentacoes = await _context.Despesas
                .Where(d => d.Data.Year == anoAtual && d.Data.Month == mesAtual)
                .Include(d => d.Categoria)
                .OrderByDescending(d => d.Data)
                .Take(5)
                .ToListAsync();

            ViewBag.Ano = anoAtual;
            ViewBag.Mes = mesAtual;
            ViewBag.Saldo = saldo;
            ViewBag.TotalReceitas = receitasMes;
            ViewBag.TotalDespesas = despesasMes;
            ViewBag.Ultimas = ultimasMovimentacoes;

            return View();
        }
    }
}
