using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var inicioMes = new DateTime(anoAtual, mesAtual, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var receitasMes = await _context.Receitas
                .Where(r => r.UserId == userId && (
                    (r.RecorrenciaId == null &&
                     r.Data >= inicioMes &&
                     r.Data <= fimMes)
                    ||
                    (r.RecorrenciaId != null &&
                     r.Data <= fimMes &&
                     (r.Recorrencia == null || r.Recorrencia.DataFim == null || r.Recorrencia.DataFim >= inicioMes))
                ))
                .SumAsync(r => (decimal?)r.Valor) ?? 0;

            var despesasDoMes = await _context.Despesas
                .Where(d => d.UserId == userId && (
                    (d.RecorrenciaId == null &&
                     d.Data >= inicioMes &&
                     d.Data <= fimMes)
                    ||
                    (d.RecorrenciaId != null &&
                     d.Data <= fimMes &&
                     (d.Recorrencia == null || d.Recorrencia.DataFim == null || d.Recorrencia.DataFim >= inicioMes))
                ))
                .Include(d => d.Categoria)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                Ano = anoAtual,
                Mes = mesAtual,
                TotalReceitas = receitasMes,
                TotalDespesas = despesasDoMes.Sum(d => d.Valor),
                UltimasDespesas = despesasDoMes
                    .Select(d => new DashboardDespesaViewModel
                    {
                        Data = d.RecorrenciaId != null
                            ? CriarDataReferenciaNoMes(d.Data, anoAtual, mesAtual)
                            : d.Data,
                        Descricao = d.Descricao,
                        CategoriaNome = d.Categoria?.Nome ?? "Sem categoria",
                        Valor = d.Valor,
                        Recorrente = d.RecorrenciaId != null
                    })
                    .OrderByDescending(d => d.Data)
                    .ThenBy(d => d.Descricao)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        private static DateTime CriarDataReferenciaNoMes(DateTime dataOriginal, int ano, int mes)
        {
            var dia = Math.Min(dataOriginal.Day, DateTime.DaysInMonth(ano, mes));
            return new DateTime(ano, mes, dia);
        }
    }
}
