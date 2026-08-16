using System.Security.Claims;

namespace ProjectFlow.Api.Infrastructure;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("The access token has no user identifier.");

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("The access token has an invalid user identifier.");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}

