using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Extensions;
using GestaoFinanceiraPessoal.Models.ViewModels;
using GestaoFinanceiraPessoal.Services;
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
        private readonly RecorrenciaService _recorrenciaService;

        public HomeController(AppDbContext context, RecorrenciaService recorrenciaService)
        {
            _context = context;
            _recorrenciaService = recorrenciaService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? ano, int? mes)
        {
            var anoAtual = ano ?? DateTime.Now.Year;
            var mesAtual = mes ?? DateTime.Now.Month;
            var inicioMes = new DateTime(anoAtual, mesAtual, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);
            var userId = User.GetRequiredUserId();

            await _recorrenciaService.MaterializarCompetenciasAteAsync(userId, anoAtual, mesAtual);

            var receitasMes = await _context.Receitas
                .Where(r => r.UserId == userId
                    && !r.EhModeloRecorrente
                    && !r.OcorrenciaCancelada
                    && r.Data >= inicioMes
                    && r.Data <= fimMes)
                .SumAsync(r => (decimal?)r.Valor) ?? 0;

            var despesasDoMes = await _context.Despesas
                .Where(d => d.UserId == userId
                    && !d.EhModeloRecorrente
                    && !d.OcorrenciaCancelada
                    && d.Data >= inicioMes
                    && d.Data <= fimMes)
                .Include(d => d.Categoria)
                .OrderByDescending(d => d.Data)
                .ThenBy(d => d.Descricao)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                Ano = anoAtual,
                Mes = mesAtual,
                TotalReceitas = receitasMes,
                TotalDespesas = await _context.Despesas
                    .Where(d => d.UserId == userId
                        && !d.EhModeloRecorrente
                        && !d.OcorrenciaCancelada
                        && d.Data >= inicioMes
                        && d.Data <= fimMes)
                    .SumAsync(d => (decimal?)d.Valor) ?? 0,
                UltimasDespesas = despesasDoMes
                    .Select(d => new DashboardDespesaViewModel
                    {
                        Data = d.Data,
                        Descricao = d.Descricao,
                        CategoriaNome = d.Categoria?.Nome ?? "Sem categoria",
                        Status = d.Status,
                        Valor = d.Valor,
                        Recorrente = d.RecorrenciaId != null
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}
