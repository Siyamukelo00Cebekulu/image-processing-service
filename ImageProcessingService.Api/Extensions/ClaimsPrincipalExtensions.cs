using System.Security.Claims;

namespace ImageProcessingService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var rawValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated user identifier is missing.");

        return Guid.Parse(rawValue);
    }
}
