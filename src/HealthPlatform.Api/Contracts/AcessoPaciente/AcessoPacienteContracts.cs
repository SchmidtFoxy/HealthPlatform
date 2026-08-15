namespace HealthPlatform.Api.Contracts.AcessoPaciente;

public sealed record CriarAcessoPacienteRequest(string? Email);

public sealed record AcessoPacienteStatusResponse(
    Guid PacienteId,
    bool PossuiAcesso,
    bool Ativado,
    string? Email);

public sealed record ConvitePacienteResponse(
    Guid PacienteId,
    string Email,
    string ActivationToken,
    bool NovoUsuario);

public sealed record AtivarPacienteRequest(
    string Email,
    string Token,
    string Senha);
