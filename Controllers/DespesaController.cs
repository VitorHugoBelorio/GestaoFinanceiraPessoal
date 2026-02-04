using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using GestaoFinanceiraPessoal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoFinanceiraPessoal.Controllers
{

    [Authorize]
    [Route("despesas")]
    public class DespesaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RecorrenciaService _recorrenciaService;

        public DespesaController(
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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var despesas = await _context.Despesas
                .Include(d => d.Categoria)
                .Include(d => d.Recorrencia)
                .Where(d => d.UserId == userId && (
                    // Variáveis (sem recorrência)
                    (d.RecorrenciaId == null &&
                     d.Data.Month == mes &&
                     d.Data.Year == ano)

                    ||

                    // Fixas (recorrentes)
                    (d.RecorrenciaId != null &&
                     d.Recorrencia.ProximaExecucao <= dataReferencia &&
                     (d.Recorrencia.DataFim == null || d.Recorrencia.DataFim >= dataReferencia))
                ))
                .ToListAsync();

            ViewBag.Mes = mes;
            ViewBag.Ano = ano;

            return View(despesas);
        }

        // FORM CREATE
        [HttpGet("novo")]
        public async Task<IActionResult> Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return View();
        }

        // CREATE
        [HttpPost("")]
        public async Task<IActionResult> Create(Despesa model, bool recorrente)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _context.Categorias
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                return View(model);
            }

            if (recorrente)
            {
                model.Recorrencia = _recorrenciaService
                    .CriarRecorrencia(model.Data);
            }

            model.UserId = userId;

            _context.Despesas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (despesa == null)
                return NotFound();

            if (despesa.Recorrencia != null)
            {
                _recorrenciaService.Desativar(despesa.Recorrencia);
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
