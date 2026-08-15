namespace HealthPlatform.Api.Contracts.PerguntasAnamnese;

public sealed record PerguntaAnamneseResponse(
    Guid Id,
    string Texto,
    string TipoResposta,
    IReadOnlyCollection<string> Opcoes,
    int Ordem,
    bool Ativa,
    DateTime CreatedAtUtc);
