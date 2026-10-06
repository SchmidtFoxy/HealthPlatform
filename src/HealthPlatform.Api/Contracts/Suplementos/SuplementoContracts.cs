namespace HealthPlatform.Api.Contracts.Suplementos;

public record UpsertSuplementoRequest(
    string Nome,
    string? Marca,
    string Categoria,
    string? Forma,
    decimal PorcaoQuantidade,
    string PorcaoUnidade,
    decimal CaloriasPorPorcao,
    decimal ProteinasGPorPorcao,
    decimal CarboidratosGPorPorcao,
    decimal GordurasGPorPorcao,
    decimal FibrasGPorPorcao,
    decimal? CafeinaMgPorPorcao,
    string? Composicao,
    string? InstrucoesUso,
    string? Observacoes);

public record SuplementoResponse(
    Guid Id,
    string Nome,
    string? Marca,
    string Categoria,
    string? Forma,
    decimal PorcaoQuantidade,
    string PorcaoUnidade,
    decimal CaloriasPorPorcao,
    decimal ProteinasGPorPorcao,
    decimal CarboidratosGPorPorcao,
    decimal GordurasGPorPorcao,
    decimal FibrasGPorPorcao,
    decimal? CafeinaMgPorPorcao,
    string? Composicao,
    string? InstrucoesUso,
    string? Observacoes,
    bool Ativo,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
