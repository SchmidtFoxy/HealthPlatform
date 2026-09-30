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
public sealed record WorkoutPeriodizationPhaseResponse(
    Guid Id, string Nome, string Tipo, int Ordem, string Status, DateOnly DataInicio, DateOnly? DataFim,
    int DuracaoSemanas, int? SemanaAtual, bool EhDeload, string? Objetivo, string? CriterioTransicao);
public sealed record WorkoutPeriodizationResponse(
    string Estado, string Microciclo, string Mesociclo, string Bloco, int? SemanaPlano, int? TotalSemanasPlano,
    string? FaseAtual, string? TipoFaseAtual, int? SemanaFaseAtual, int? TotalSemanasFaseAtual, bool DeloadPlanejado,
    IReadOnlyCollection<WorkoutPeriodizationPhaseResponse> Fases, string RegraDeUso);
public sealed record WorkoutIntelligenceResponse(string Versao,int PeriodoDias,Guid? PlanoId,string? Plano,string? StatusPlano,int SessoesPlanejadas,int ItensPlanejados,int SessoesExecutadas,int ItensExecutados,WorkoutIntelligenceSummaryResponse Resumo,IReadOnlyCollection<WorkoutIntelligenceDimensionResponse> Dimensoes,IReadOnlyCollection<WorkoutIntelligenceComparisonResponse> Comparacoes,IReadOnlyCollection<WorkoutProgressionRegressionSignalResponse> SinaisProgressaoRegressao,WorkoutPeriodizationResponse Periodizacao,IReadOnlyCollection<string> ProximasCamadas,string RegraDeUso);
