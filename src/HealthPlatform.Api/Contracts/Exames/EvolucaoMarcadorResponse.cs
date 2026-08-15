namespace HealthPlatform.Api.Contracts.Exames;

public record EvolucaoMarcadorPontoResponse(
    Guid ExameId,
    DateTime DataColetaUtc,
    decimal Valor,
    string? Unidade,
    decimal? ReferenciaMinima,
    decimal? ReferenciaMaxima,
    string? Situacao,
    string? Laboratorio);

public record EvolucaoMarcadorResponse(
    Guid MarcadorId,
    string MarcadorNome,
    string? UnidadePadrao,
    IReadOnlyCollection<EvolucaoMarcadorPontoResponse> Pontos);
