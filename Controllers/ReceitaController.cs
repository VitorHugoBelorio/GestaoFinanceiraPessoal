using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using GestaoFinanceiraPessoal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Route("receitas")]
    public class ReceitaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RecorrenciaService _recorrenciaService;

        public ReceitaController(
            AppDbContext context,
            RecorrenciaService recorrenciaService)
        {
            _context = context;
            _recorrenciaService = recorrenciaService;
        }

        // LISTAGEM
        [HttpGet("")]
        public async Task<IActionResult> Index(int mes, int ano)
        {
            if (mes == 0) mes = DateTime.Now.Month;
            if (ano == 0) ano = DateTime.Now.Year;

            var dataReferencia = new DateTime(ano, mes, 1);

            var receitas = await _context.Receitas
                .Include(r => r.Recorrencia)
                .Where(r =>
                    // Variáveis
                    (r.RecorrenciaId == null &&
                     r.Data.Month == mes &&
                     r.Data.Year == ano)

                    ||

                    // Fixas
                    (r.RecorrenciaId != null &&
                     r.Recorrencia.ProximaExecucao <= dataReferencia &&
                     (r.Recorrencia.DataFim == null || r.Recorrencia.DataFim >= dataReferencia))
                )
                .ToListAsync();

            ViewBag.Mes = mes;
            ViewBag.Ano = ano;

            return View(receitas);
        }

        // FORM CREATE
        [HttpGet("novo")]
        public IActionResult Create() => View();

        // CREATE
        [HttpPost("novo")]
        public async Task<IActionResult> Create(Receita model, bool recorrente)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (recorrente)
            {
                model.Recorrencia = _recorrenciaService
                    .CriarRecorrencia(model.Data);
            }

            _context.Receitas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpGet("excluir/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receita == null)
                return NotFound();

            if (receita.Recorrencia != null)
            {
                _recorrenciaService.Desativar(receita.Recorrencia);
            }
            else
            {
                _context.Receitas.Remove(receita);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
