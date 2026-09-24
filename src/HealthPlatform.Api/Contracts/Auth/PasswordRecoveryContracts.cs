namespace HealthPlatform.Api.Contracts.Auth;

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NovaSenha,
    string ConfirmacaoNovaSenha);
