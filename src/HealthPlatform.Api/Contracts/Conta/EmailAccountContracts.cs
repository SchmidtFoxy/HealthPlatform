namespace HealthPlatform.Api.Contracts.Conta;

public sealed record EmailAccountStatusResponse(
    string? EmailPrincipal,
    bool Confirmado,
    bool PodeSolicitarConfirmacao);

public sealed record RequestEmailChangeRequest(
    string NovoEmail,
    string SenhaAtual);

public sealed record ConfirmEmailRequest(
    Guid UsuarioId,
    string Token);

public sealed record ConfirmEmailChangeRequest(
    Guid UsuarioId,
    string NovoEmail,
    string Token);
