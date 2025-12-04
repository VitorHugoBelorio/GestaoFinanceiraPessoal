using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using GestaoFinanceiraPessoal.Enuns;

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

            var receitas = await _context.Receitas
                .Where(r => r.Mes == mes && r.Ano == ano)
                .ToListAsync();

            ViewBag.Mes = mes;
            ViewBag.Ano = ano;

            return View(receitas);
        }

        [HttpGet("novo")]
        public IActionResult Create() => View();

        [HttpPost("novo")]
        public async Task<IActionResult> Create(Receita model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Receitas.Add(model);
            await _context.SaveChangesAsync();

            if (model.Tipo == ETiposReceita.Fixo)
            {
                var mes = model.Mes + 1;
                var ano = model.Ano;

                if (mes == 13)
                {
                    mes = 1;
                    ano++;
                }

                var repeticao = new Receita
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Tipo = model.Tipo,
                    Mes = mes,
                    Ano = ano
                };

                _context.Receitas.Add(repeticao);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("excluir/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var receita = await _context.Receitas.FindAsync(id);
            if (receita == null) return NotFound();

            _context.Receitas.Remove(receita);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
