namespace HealthPlatform.Api.Contracts.Auth;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Nome, string TipoUsuario);
