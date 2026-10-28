using System.Security.Claims;
using GestaoFinanceiraPessoal.Api.Contracts.Auth;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Extensions;
using GestaoFinanceiraPessoal.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register-and-onboard")]
    public async Task<ActionResult<AuthSessionResponse>> Register(
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Account.Email);

        if (await dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Não foi possível concluir o cadastro.",
                Detail = "Já existe uma conta cadastrada com esse e-mail.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = new User
        {
            Name = request.Account.Name.Trim(),
            Email = request.Account.Email.Trim(),
            NormalizedEmail = normalizedEmail
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Account.Password);

        dbContext.Users.Add(user);
        OnboardingSetupService.AddInitialSetup(dbContext, user, request.Setup);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SignInAsync(user);

        return Created("/api/auth/me", ToSessionResponse(user, true));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthSessionResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .Include(existingUser => existingUser.FinancialProfile)
            .FirstOrDefaultAsync(
                existingUser => existingUser.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "E-mail ou senha inválidos.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "E-mail ou senha inválidos.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SignInAsync(user);
        return Ok(ToSessionResponse(user, user.FinancialProfile is not null));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthSessionResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var user = await dbContext.Users
            .Include(existingUser => existingUser.FinancialProfile)
            .FirstAsync(existingUser => existingUser.Id == userId, cancellationToken);

        return Ok(ToSessionResponse(user, user.FinancialProfile is not null));
    }

    [Authorize]
    [HttpPut("account")]
    public async Task<ActionResult<AuthSessionResponse>> UpdateAccount(
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var user = await dbContext.Users
            .Include(existingUser => existingUser.FinancialProfile)
            .FirstAsync(existingUser => existingUser.Id == userId, cancellationToken);

        if (user.FinancialProfile is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Os dados do cadastro só podem ser revisados durante a configuração inicial.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword)
            == PasswordVerificationResult.Failed)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Senha atual incorreta.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(
            existingUser => existingUser.Id != userId && existingUser.NormalizedEmail == normalizedEmail,
            cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Este e-mail já está em uso.",
                Status = StatusCodes.Status409Conflict
            });
        }

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SignInAsync(user);
        return Ok(ToSessionResponse(user, false));
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();

    private static AuthSessionResponse ToSessionResponse(User user, bool onboardingCompleted) =>
        new(user.Id, user.Name, user.Email, onboardingCompleted);
}
