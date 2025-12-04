using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Enuns;
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

            var despesas = await _context.Despesas
                .Include(c => c.Categoria)
                .Where(d => d.Data.Month == mes && d.Data.Year == ano)
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
        public async Task<IActionResult> Create(Despesa model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _context.Categorias.ToListAsync();
                return View(model);
            }

            _context.Despesas.Add(model);
            await _context.SaveChangesAsync();

            // DESPESA FIXA
            if (model.Tipo == ETiposDespesas.Fixo)
            {
                var next = model.Data.AddMonths(1);

                var repeticao = new Despesa
                {
                    Valor = model.Valor,
                    Descricao = model.Descricao,
                    CategoriaId = model.CategoriaId,
                    Tipo = model.Tipo,
                    Data = new DateTime(next.Year, next.Month, model.Data.Day)
                };

                _context.Despesas.Add(repeticao);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/editar")]
        public async Task<IActionResult> Edit(int id)
        {
            var despesa = await _context.Despesas.FindAsync(id);
            if (despesa == null) return NotFound();

            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View(despesa);
        }

        [HttpPost("{id:int}")]
        public async Task<IActionResult> Edit(int id, Despesa model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _context.Categorias.ToListAsync();
                return View(model);
            }

            _context.Despesas.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var despesa = await _context.Despesas.FindAsync(id);
            if (despesa == null) return NotFound();

            _context.Despesas.Remove(despesa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
