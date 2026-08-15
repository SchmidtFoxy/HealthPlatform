namespace HealthPlatform.Api.Contracts.Relatorios;

public sealed record CreateRelatorioClinicoRequest(
    DateTime? DataInicioUtc,
    DateTime? DataFimUtc,
    string? Titulo,
    string? ConclusaoMedica);
