using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Extensions;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Authorize]
    [Route("categorias")]
    public class CategoriaController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetRequiredUserId();
            return View(await _context.Categorias
                .Where(c => c.UserId == userId)
                .ToListAsync());
        }

        [HttpGet("novo")]
        public IActionResult Create() => View();

        [HttpPost("novo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = User.GetRequiredUserId();
            model.UserId = userId;

            _context.Categorias.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.GetRequiredUserId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            return categoria == null ? NotFound() : View(categoria);
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.GetRequiredUserId();
            var existing = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (existing == null) return NotFound();

            existing.Nome = model.Nome;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetRequiredUserId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (categoria == null) return NotFound();

            var possuiDespesas = await _context.Despesas
                .AnyAsync(d => d.CategoriaId == id && d.UserId == userId);

            if (possuiDespesas)
            {
                TempData["Error"] = "Não é possível excluir a categoria porque ela já está vinculada a despesas.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Categoria excluída com sucesso.";
            return RedirectToAction(nameof(Index));
        }
    }
}

