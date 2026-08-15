namespace HealthPlatform.Api.Contracts.Timeline;

public sealed record TimelineItemResponse(
    string Tipo,
    Guid Id,
    DateTime DataUtc,
    string Titulo,
    string? Resumo,
    object Detalhes);
