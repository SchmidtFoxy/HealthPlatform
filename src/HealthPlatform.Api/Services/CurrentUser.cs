using System.Security.Claims;

namespace HealthPlatform.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub"), out var id)
        ? id : throw new UnauthorizedAccessException("Usuario nao autenticado.");

    public Guid OrganizationId => Guid.TryParse(User?.FindFirstValue("organization_id"), out var id)
        ? id : throw new UnauthorizedAccessException("Organizacao nao encontrada no token.");
}
