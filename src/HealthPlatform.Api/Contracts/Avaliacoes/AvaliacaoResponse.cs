namespace HealthPlatform.Api.Contracts.Avaliacoes;

public sealed record AvaliacaoResponse(
    Guid Id,
    Guid PacienteId,
    Guid? ConsultaId,
    DateTime DataUtc,
    decimal? PesoKg,
    decimal? AlturaM,
    decimal? Imc,
    decimal? PercentualGordura,
    decimal? MassaMagraKg,
    decimal? MassaGordaKg,
    decimal? CinturaCm,
    decimal? AbdomenCm,
    decimal? QuadrilCm,
    int? PressaoSistolica,
    int? PressaoDiastolica,
    int? FrequenciaCardiaca,
    DateTime CreatedAtUtc);
