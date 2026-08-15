namespace HealthPlatform.Api.Contracts.Anamneses;

public sealed record RespostaAnamneseResponse(
    Guid PerguntaId,
    string Pergunta,
    string TipoResposta,
    string? Resposta);
