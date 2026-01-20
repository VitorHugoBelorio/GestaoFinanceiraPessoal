using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return View(await _context.Categorias.ToListAsync());
        }

        [HttpGet("novo")]
        public IActionResult Create() => View();

        [HttpPost("")]
        public async Task<IActionResult> Create(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Categorias.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/editar")]
        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            return categoria == null ? NotFound() : View(categoria);
        }

        [HttpPost("{id:int}")]
        public async Task<IActionResult> Edit(int id, Categoria model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Categorias.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
