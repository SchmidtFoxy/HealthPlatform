namespace HealthPlatform.Api.Contracts.Consultas;

public sealed record ConsultaResponse(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    string ProfissionalNome,
    DateTime DataHoraUtc,
    string? Motivo,
    string? QueixaPrincipal,
    string? Evolucao,
    string? Conduta,
    string? Orientacoes,
    string Status,
    bool PossuiAvaliacao,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
