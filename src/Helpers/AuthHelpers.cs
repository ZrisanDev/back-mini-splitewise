using System.Security.Claims;

namespace back_api_splitwise.src.Helpers;

public static class AuthHelpers
{
    /// <summary>
    /// Extracts the authenticated user's ID from JWT claims.
    /// Checks both "sub" (raw JWT claim) and ClaimTypes.NameIdentifier (mapped by ASP.NET middleware).
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("sub")
                     ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("Token inválido: no se encontró el ID de usuario.");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Token inválido: el ID de usuario no es un GUID válido.");

        return userId;
    }
}
