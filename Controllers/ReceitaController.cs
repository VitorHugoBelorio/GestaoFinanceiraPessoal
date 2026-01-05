using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Enums;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Route("receitas")]
    public class ReceitaController : Controller
    {
        private readonly AppDbContext _context;

        public ReceitaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int mes, int ano)
        {
            if (mes == 0) mes = DateTime.Now.Month;
            if (ano == 0) ano = DateTime.Now.Year;

            var dataReferencia = new DateTime(ano, mes, 1);

            var receitas = await _context.Receitas
                .Include(r => r.Recorrencia)
                .Where(r =>
                    // Variáveis (sem recorrência)
                    (r.RecorrenciaId == null &&
                     r.Data.Month == mes &&
                     r.Data.Year == ano)

                    ||

                    // Fixas (recorrentes)
                    (r.RecorrenciaId != null &&
                     r.Recorrencia.Ativa &&
                     r.Recorrencia.DataInicio <= dataReferencia &&
                     (r.Recorrencia.DataFim == null || r.Recorrencia.DataFim >= dataReferencia))
                )
                .ToListAsync();

            ViewBag.Mes = mes;
            ViewBag.Ano = ano;

            return View(receitas);
        }

        [HttpGet("novo")]
        public IActionResult Create() => View();

        [HttpPost("novo")]
        public async Task<IActionResult> Create(Receita model, bool recorrente)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (recorrente)
            {
                var recorrencia = new Recorrencia
                {
                    DataInicio = model.Data,
                    Tipo = ETipoRecorrencia.Mensal,
                    Ativa = true
                };

                _context.Recorrencias.Add(recorrencia);
                await _context.SaveChangesAsync();

                model.RecorrenciaId = recorrencia.Id;
            }

            _context.Receitas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("excluir/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receita == null) return NotFound();

            // Se tiver recorrência, apenas desativa
            if (receita.Recorrencia != null)
            {
                receita.Recorrencia.Ativa = false;
                _context.Recorrencias.Update(receita.Recorrencia);
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
