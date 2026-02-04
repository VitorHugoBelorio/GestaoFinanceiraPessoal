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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var receitas = await _context.Receitas
                .Include(r => r.Recorrencia)
                .Where(r => r.UserId == userId && (
                    // Variáveis
                    (r.RecorrenciaId == null &&
                     r.Data.Month == mes &&
                     r.Data.Year == ano)

                    ||

                    // Fixas
                    (r.RecorrenciaId != null &&
                     r.Recorrencia.ProximaExecucao <= dataReferencia &&
                     (r.Recorrencia.DataFim == null || r.Recorrencia.DataFim >= dataReferencia))
                ))
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Receita model, bool recorrente)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (recorrente)
            {
                model.Recorrencia = _recorrenciaService
                    .CriarRecorrencia(model.Data);
            }

            model.UserId = userId;

            _context.Receitas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        [HttpGet("editar/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var receita = await _context.Receitas
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (receita == null) return NotFound();
            return View(receita);
        }

        // EDIT (POST)
        [HttpPost("editar/{id:int}")] // Usado por limitação do HTML que apenas aceita os verbos GET e POST, por se tratar de um projeto simples acho melhor deixar assim.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Receita model, bool recorrente)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var existing = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (existing == null) return NotFound();

            existing.Descricao = model.Descricao;
            existing.Valor = model.Valor;
            existing.Data = model.Data;

            if (recorrente)
            {
                if (existing.Recorrencia == null)
                    existing.Recorrencia = _recorrenciaService.CriarRecorrencia(existing.Data);
            }
            else
            {
                if (existing.Recorrencia != null)
                    _recorrenciaService.Desativar(existing.Recorrencia);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpGet("excluir/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

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
