namespace HealthPlatform.Api.Contracts.PerformancePassport;

public sealed record AthletePerformancePassportDomainResponse(
    string Codigo, string Nome, string Estado, string Origem, int Registros, string Observacao);

public sealed record AthletePerformancePassportRecordResponse(
    Guid ExercicioId, string Exercicio, string? GrupoMuscular, string Tipo, decimal? Valor, string? Unidade,
    DateTime? DataUtc, bool Recente, int Registros, string Fonte);

public sealed record AthletePerformancePassportResponse(
    string Versao, int DiasObservados, int TreinosObservados, int RecordesRecentes, string Estado,
    IReadOnlyCollection<AthletePerformancePassportDomainResponse> Dominios,
    IReadOnlyCollection<AthletePerformancePassportRecordResponse> MelhoresMarcas,
    string RegraDeUso);
