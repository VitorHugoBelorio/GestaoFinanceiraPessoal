using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Enums;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Route("despesas")]
    public class DespesaController : Controller
    {
        private readonly AppDbContext _context;

        public DespesaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int mes, int ano)
        {
            if (mes == 0) mes = DateTime.Now.Month;
            if (ano == 0) ano = DateTime.Now.Year;

            var dataReferencia = new DateTime(ano, mes, 1);

            var despesas = await _context.Despesas
                .Include(d => d.Categoria)
                .Include(d => d.Recorrencia)
                .Where(d =>
                    // Variáveis
                    (d.RecorrenciaId == null &&
                     d.Data.Month == mes &&
                     d.Data.Year == ano)

                    ||

                    // Fixas
                    (d.RecorrenciaId != null &&
                     d.Recorrencia.Ativa &&
                     d.Recorrencia.DataInicio <= dataReferencia &&
                     (d.Recorrencia.DataFim == null || d.Recorrencia.DataFim >= dataReferencia))
                )
                .ToListAsync();

            ViewBag.Mes = mes;
            ViewBag.Ano = ano;

            return View(despesas);
        }

        [HttpGet("novo")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View();
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(Despesa model, bool recorrente)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _context.Categorias.ToListAsync();
                return View(model);
            }

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

            _context.Despesas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (despesa == null) return NotFound();

            if (despesa.Recorrencia != null)
            {
                despesa.Recorrencia.Ativa = false;
                _context.Recorrencias.Update(despesa.Recorrencia);
            }
            else
            {
                _context.Despesas.Remove(despesa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
