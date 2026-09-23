namespace HealthPlatform.Api.Contracts.Timeline;

public sealed record TimelineItemResponse(
    string Tipo,
    Guid Id,
    DateTime DataUtc,
    string Titulo,
    string? Resumo,
    object Detalhes,
    string Categoria = "Clinico",
    int Prioridade = 0,
    string? ContextoChave = null,
    string Fonte = "Prontuario");
