namespace HealthPlatform.Api.Contracts.PerguntasAnamnese;

public sealed record CreatePerguntaAnamneseRequest(
    string Texto,
    string? TipoResposta,
    IReadOnlyCollection<string>? Opcoes,
    int? Ordem);
