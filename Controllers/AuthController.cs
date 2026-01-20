using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /auth/login
        [HttpGet("login")]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }


        // POST: /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var passwordHash = HashPassword(password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == username &&
                    u.PasswordHash == passwordHash);

            if (user == null)
            {
                ViewBag.Error = "Usuário ou senha inválidos";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToAction("Index", "Home");
        }

        // GET: /auth/logout
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        // Hash simples (SHA256)
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // GET: /auth/register
        [HttpGet("register")]
        public IActionResult Register() => View();

        // POST: /auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            string username,
            string password,
            string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "As senhas não conferem";
                return View();
            }

            var exists = await _context.Users
                .AnyAsync(u => u.Username == username);

            if (exists)
            {
                ViewBag.Error = "Usuário já existe";
                return View();
            }

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET
        [HttpGet("forgot-password")]
        public IActionResult ForgotPassword() => View();

        // POST (placeholder)
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(string username)
        {
            // lógica futura (email / token)
            ViewBag.Error = "Funcionalidade em desenvolvimento";
            return View();
        }

    }
}
