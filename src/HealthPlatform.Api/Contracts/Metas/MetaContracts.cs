namespace HealthPlatform.Api.Contracts.Metas;

public record UpsertMetaRequest(
    string Nome,
    string Tipo,
    decimal? ValorObjetivo,
    string? Unidade,
    string Frequencia,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Observacoes);

public record RegistrarMetaRequest(DateOnly Data, decimal? Valor, bool? Concluida, string? Observacao);

public record RegistroMetaResponse(Guid Id, DateOnly Data, decimal? Valor, bool? Concluida, string? Observacao, DateTime CreatedAtUtc);

public record MetaPacienteResponse(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    string ProfissionalNome,
    string Nome,
    string Tipo,
    decimal? ValorObjetivo,
    string? Unidade,
    string Frequencia,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Status,
    string? Observacoes,
    decimal? ProgressoHojePercentual,
    RegistroMetaResponse? RegistroHoje,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
