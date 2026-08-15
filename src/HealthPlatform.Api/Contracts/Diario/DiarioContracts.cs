namespace HealthPlatform.Api.Contracts.Diario;

public record UpsertRegistroDiarioRequest(
    DateTime DataHoraUtc,
    string Tipo,
    string? Descricao,
    decimal? ValorNumerico,
    string? Unidade,
    int? Escala,
    string? ImagemUrl);

public record RegistroDiarioResponse(
    Guid Id,
    Guid PacienteId,
    DateTime DataHoraUtc,
    string Tipo,
    string? Descricao,
    decimal? ValorNumerico,
    string? Unidade,
    int? Escala,
    string? ImagemUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record ResumoMetaHojeResponse(Guid MetaId, string Nome, string Tipo, decimal? ValorObjetivo, string? Unidade, decimal? ValorHoje, bool? Concluida, decimal? ProgressoPercentual);
public record ResumoDiaPacienteResponse(DateOnly Data, IReadOnlyCollection<ResumoMetaHojeResponse> Metas, IReadOnlyCollection<RegistroDiarioResponse> Registros, int MetasAtivas, int MetasConcluidas, decimal PercentualConclusao);
