namespace HealthPlatform.Api.Contracts.Avaliacoes;

public sealed record CreateAvaliacaoRequest(
    Guid? ConsultaId,
    DateTime? DataUtc,
    decimal? PesoKg,
    decimal? AlturaM,
    decimal? PercentualGordura,
    decimal? MassaMagraKg,
    decimal? MassaGordaKg,
    decimal? CinturaCm,
    decimal? AbdomenCm,
    decimal? QuadrilCm,
    int? PressaoSistolica,
    int? PressaoDiastolica,
    int? FrequenciaCardiaca);
