using System.Security.Claims;

namespace GestaoFinanceiraPessoal.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetRequiredUserId(this ClaimsPrincipal user)
        {
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException("Usuário autenticado inválido.");
            }

            return userId;
        }
    }
}
