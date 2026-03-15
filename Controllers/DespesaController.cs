using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using GestaoFinanceiraPessoal.Models.ViewModels;
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

            var inicioMes = new DateTime(ano, mes, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var despesas = await _context.Despesas
                .Include(d => d.Categoria)
                .Include(d => d.Recorrencia)
                .Where(d => d.UserId == userId && (
                    (d.RecorrenciaId == null &&
                     d.Data >= inicioMes &&
                     d.Data <= fimMes)
                    ||
                    (d.RecorrenciaId != null &&
                     d.Data <= fimMes &&
                     (d.Recorrencia == null || d.Recorrencia.DataFim == null || d.Recorrencia.DataFim >= inicioMes))
                ))
                .OrderBy(d => d.Data)
                .ToListAsync();

            var model = new DespesaIndexViewModel
            {
                Ano = ano,
                Mes = mes,
                Total = despesas.Sum(d => d.Valor),
                Itens = despesas
                    .Select(d => new DespesaListaItemViewModel
                    {
                        Id = d.Id,
                        Descricao = d.Descricao,
                        CategoriaNome = d.Categoria?.Nome ?? "Sem categoria",
                        Valor = d.Valor,
                        DataExibicao = d.RecorrenciaId != null
                            ? CriarDataReferenciaNoMes(d.Data, ano, mes)
                            : d.Data,
                        Recorrente = d.RecorrenciaId != null
                    })
                    .OrderBy(d => d.DataExibicao)
                    .ThenBy(d => d.Descricao)
                    .ToList()
            };

            return View(model);
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
        [HttpPost("novo")]
        [ValidateAntiForgeryToken]
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

        // EDIT (GET)
        [HttpGet("editar/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var despesa = await _context.Despesas
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (despesa == null)
                return NotFound();

            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(despesa);
        }

        // EDIT (POST)
        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Despesa model, bool recorrente)
        {
            if (id != model.Id)
                return BadRequest();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _context.Categorias
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                return View(model);
            }

            var existing = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (existing == null)
                return NotFound();

            existing.Descricao = model.Descricao;
            existing.Valor = model.Valor;
            existing.Data = model.Data;
            existing.CategoriaId = model.CategoriaId;

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

        private static DateTime CriarDataReferenciaNoMes(DateTime dataOriginal, int ano, int mes)
        {
            var dia = Math.Min(dataOriginal.Day, DateTime.DaysInMonth(ano, mes));
            return new DateTime(ano, mes, dia);
        }
    }
}
