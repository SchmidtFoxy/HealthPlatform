namespace HealthPlatform.Api.Contracts.Consultas;

public sealed record UpdateConsultaRequest(
    DateTime DataHoraUtc,
    string? Motivo,
    string? QueixaPrincipal,
    string? Evolucao,
    string? Conduta,
    string? Orientacoes,
    string? Status);
