namespace HealthPlatform.Api.Contracts.Exames;

public record ResultadoExameResponse(
    Guid Id,
    Guid MarcadorId,
    string MarcadorNome,
    string? Categoria,
    decimal? ValorNumerico,
    string? ValorTexto,
    string? Unidade,
    decimal? ReferenciaMinima,
    decimal? ReferenciaMaxima,
    string? ReferenciaTexto,
    string? Situacao,
    string? Observacao);
