using System.Security.Claims;

namespace GestaoFinanceiraPessoal.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("A sessão do usuário é inválida.");
        }

        return userId;
    }
}
