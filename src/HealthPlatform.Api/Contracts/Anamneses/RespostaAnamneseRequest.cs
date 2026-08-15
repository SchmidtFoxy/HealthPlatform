namespace HealthPlatform.Api.Contracts.Anamneses;

public sealed record RespostaAnamneseRequest(Guid PerguntaId, string? Resposta);
