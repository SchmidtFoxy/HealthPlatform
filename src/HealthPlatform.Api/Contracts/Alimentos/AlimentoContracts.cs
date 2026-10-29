namespace HealthPlatform.Api.Contracts.Alimentos;

public record UpsertAlimentoRequest(
    string Nome,
    string? Categoria,
    decimal CaloriasPor100g,
    decimal ProteinasPor100g,
    decimal CarboidratosPor100g,
    decimal GordurasPor100g,
    decimal FibrasPor100g,
    string? UnidadeNome = null,
    decimal? GramasPorUnidade = null,
    decimal? GramasPorMl = null);

public record AlimentoResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    decimal CaloriasPor100g,
    decimal ProteinasPor100g,
    decimal CarboidratosPor100g,
    decimal GordurasPor100g,
    decimal FibrasPor100g,
    bool Ativo,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? UnidadeNome,
    decimal? GramasPorUnidade,
    decimal? GramasPorMl);
