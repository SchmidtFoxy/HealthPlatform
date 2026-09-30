namespace HealthPlatform.Api.Contracts.WorkoutIntelligence;

public sealed record WorkoutIntelligenceDimensionResponse(string Codigo,string Nome,string Estado,string Origem,int ItensComDado,int TotalItens,string Observacao);
public sealed record WorkoutIntelligenceSummaryResponse(int SeriesPrescritasNoPlano,int SeriesRealizadasNoPeriodo,decimal? RpeMedioSessao,int ItensComCargaPrescrita,int ItensComCargaRealizada,int ItensComRepeticoesPrescritas,int ItensComRepeticoesRealizadas,int ItensComExecucaoComparavel,int ItensComDiferencasRegistradas);
public sealed record WorkoutIntelligenceComparisonResponse(
    Guid ItemTreinoId, string Sessao, string Exercicio, int ExecucoesNoPeriodo, DateTime? UltimaExecucaoUtc, string Estado,
    int SeriesPrescritas, int? SeriesRealizadas, string RepeticoesPrescritas, string? RepeticoesRealizadas,
    decimal? CargaPrescrita, string? UnidadeCargaPrescrita, decimal? CargaRealizada, string? UnidadeCargaRealizada,
    int? RirAlvo, int? RirRealizado, string? CadenciaPrescrita, string? CadenciaRealizada,
    string? TecnicaPrescrita, string? TecnicaExecutada, string? TecnicaCodigoPrescrita, string? TecnicaCodigoExecutada,
    string? TecnicaParametrosPrescritos, string? TecnicaParametrosExecutados, IReadOnlyCollection<string> Diferencas);
public sealed record WorkoutProgressionRegressionSignalResponse(
    Guid ItemTreinoId, string Sessao, string Exercicio, string Estado, string Direcao, int ExecucoesAnalisadas,
    IReadOnlyCollection<string> Evidencias, string Sugestao, string RegraDeRevisao);
public sealed record WorkoutIntelligenceResponse(string Versao,int PeriodoDias,Guid? PlanoId,string? Plano,string? StatusPlano,int SessoesPlanejadas,int ItensPlanejados,int SessoesExecutadas,int ItensExecutados,WorkoutIntelligenceSummaryResponse Resumo,IReadOnlyCollection<WorkoutIntelligenceDimensionResponse> Dimensoes,IReadOnlyCollection<WorkoutIntelligenceComparisonResponse> Comparacoes,IReadOnlyCollection<WorkoutProgressionRegressionSignalResponse> SinaisProgressaoRegressao,IReadOnlyCollection<string> ProximasCamadas,string RegraDeUso);
