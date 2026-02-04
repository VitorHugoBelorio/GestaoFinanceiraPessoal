using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Authorize]
    [Route("categoria")]
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return View(await _context.Categorias
                .Where(c => c.UserId == userId)
                .ToListAsync());
        }

        [HttpGet("novo")]
        public IActionResult Create() => View();

        [HttpPost("")]
        public async Task<IActionResult> Create(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            model.UserId = userId;

            _context.Categorias.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/editar")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            return categoria == null ? NotFound() : View(categoria);
        }

        [HttpPost("{id:int}")]
        public async Task<IActionResult> Edit(int id, Categoria model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (categoria == null) return NotFound();

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
