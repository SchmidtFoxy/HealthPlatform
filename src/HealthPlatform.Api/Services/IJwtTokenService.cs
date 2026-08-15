using HealthPlatform.Infrastructure.Identity;

namespace HealthPlatform.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) Create(Usuario usuario, IReadOnlyCollection<string> roles);
}
