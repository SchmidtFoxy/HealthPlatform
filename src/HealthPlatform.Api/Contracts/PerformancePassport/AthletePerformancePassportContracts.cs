namespace HealthPlatform.Api.Contracts.PerformancePassport;

public sealed record AthletePerformancePassportDomainResponse(
    string Codigo, string Nome, string Estado, string Origem, int Registros, string Observacao);

public sealed record AthletePerformancePassportRecordResponse(
    Guid ExercicioId, string Exercicio, string? GrupoMuscular, string Tipo, decimal? Valor, string? Unidade,
    DateTime? DataUtc, bool Recente, int Registros, string Fonte);


public sealed record AthletePerformanceRecordResponse(
    Guid ExercicioId,
    string Exercicio,
    string? GrupoMuscular,
    string Tipo,
    string Natureza,
    decimal Valor,
    string Unidade,
    DateTime DataUtc,
    bool Recente,
    int RegistrosComparaveis,
    decimal? EvolucaoDesdePrimeiroPercentual,
    string Criterio,
    string Fonte);


public sealed record AthleteTimedPerformanceResponse(
    Guid SessaoTreinoId,
    string Sessao,
    int RegistrosComparaveis,
    int DuracaoRecenteMinutos,
    int MenorDuracaoMinutos,
    int MaiorDuracaoMinutos,
    decimal MediaDuracaoMinutos,
    DateTime UltimaExecucaoUtc,
    string OrigemDuracaoRecente,
    string RegraDeLeitura);


public sealed record AthleteCompetitionTestResultResponse(
    Guid EventoId,
    string Categoria,
    string Eixo,
    string Descricao,
    DateTime DataAplicacaoUtc,
    string Status,
    string? Observacoes,
    Guid? CicloEsportivoPacienteId,
    string Natureza,
    string Fonte,
    string RegraDeLeitura);


public sealed record AthleteSkillMilestoneResponse(
    Guid EventoId,
    string Categoria,
    string Eixo,
    string Descricao,
    DateTime DataAplicacaoUtc,
    string Status,
    string? Observacoes,
    Guid? CicloEsportivoPacienteId,
    string Natureza,
    string Fonte,
    string RegraDeLeitura);


public sealed record AthletePerformanceEvolutionPointResponse(
    string Dominio,
    string Referencia,
    string Medida,
    decimal ValorInicial,
    decimal ValorAtual,
    string Unidade,
    decimal VariacaoAbsoluta,
    decimal? VariacaoPercentual,
    DateTime DataInicialUtc,
    DateTime DataAtualUtc,
    int RegistrosComparaveis,
    string InterpretacaoPermitida);

public sealed record AthletePerformanceEvolutionResponse(
    int DiasObservados,
    IReadOnlyCollection<AthletePerformanceEvolutionPointResponse> Pontos,
    int CargasComparaveis,
    int SessoesTemporaisComparaveis,
    int ResultadosSupervisionados,
    int HabilidadesMarcos,
    string RegraDeLeitura);


public sealed record ProgressIntelligenceSignalResponse(
    string Dominio,
    string Referencia,
    string Tipo,
    string DirecaoDescritiva,
    decimal? VariacaoPercentual,
    int RegistrosComparaveis,
    DateTime? DataInicialUtc,
    DateTime? DataAtualUtc,
    string Evidencia,
    string LimiteInterpretativo);






public sealed record ProgressObservationSummaryResponse(
    int DiasObservados,
    int SinaisDescritivos,
    int ContextosDisponiveis,
    int EventosTimeline,
    int JanelasTemporais,
    int DiasObservadosNoMapa,
    int DiasComMultiplosDominios,
    int DiasComMultiplasReferencias,
    int ReferenciasDistintasTimeline,
    string CoberturaGeral,
    string LeituraPermitida);

public sealed record CrossSignalObservationDayResponse(
    DateOnly Data,
    int EventosObservados,
    int EventosCarga,
    int EventosTempo,
    int ReferenciasDistintas,
    IReadOnlyCollection<string> DominiosObservados,
    IReadOnlyCollection<string> ReferenciasObservadas,
    string LeituraPermitida);

public sealed record CrossSignalObservationMapResponse(
    int DiasObservados,
    IReadOnlyCollection<CrossSignalObservationDayResponse> Dias,
    int DiasComMultiplosDominios,
    int DiasComMultiplasReferencias,
    string RegraDeUso);

public sealed record ProgressEvidenceWindowItemResponse(
    string Janela,
    int Dias,
    DateTime InicioUtc,
    DateTime FimUtc,
    int EventosObservados,
    int EventosCarga,
    int EventosTempo,
    int ReferenciasDistintas,
    string CoberturaDescritiva);

public sealed record ProgressEvidenceWindowsResponse(
    int DiasObservados,
    IReadOnlyCollection<ProgressEvidenceWindowItemResponse> Janelas,
    string RegraDeUso);

public sealed record MultiSignalTimelineEventResponse(
    string Dominio,
    string Referencia,
    DateTime DataUtc,
    string Momento,
    string Medida,
    decimal Valor,
    string Unidade,
    int RegistrosComparaveis,
    string Recencia,
    string OrigemEvidencia);

public sealed record MultiSignalTimelineResponse(
    int DiasObservados,
    IReadOnlyCollection<MultiSignalTimelineEventResponse> Eventos,
    int EventosCarga,
    int EventosTempo,
    int ReferenciasDistintas,
    string RegraDeUso);

public sealed record ProgressSignalContextResponse(
    string Dominio,
    string Referencia,
    string Recencia,
    int DiasDesdeUltimoRegistro,
    int RegistrosComparaveis,
    int DiasCobertos,
    string DensidadeObservacional,
    string OrigemEvidencia,
    string ContextoDeLeitura);

public sealed record ProgressSignalContextSummaryResponse(
    int DiasObservados,
    IReadOnlyCollection<ProgressSignalContextResponse> Contextos,
    int SinaisRecentes,
    int SinaisIntermediarios,
    int SinaisAntigos,
    string RegraDeUso);

public sealed record ProgressIntelligenceFoundationResponse(
    int DiasObservados,
    IReadOnlyCollection<ProgressIntelligenceSignalResponse> Sinais,
    int SinaisCarga,
    int SinaisTempo,
    int RegistrosSupervisionados,
    string RegraDeUso);

public sealed record AthletePerformancePassportResponse(
    string Versao, int DiasObservados, int TreinosObservados, int RecordesRecentes, string Estado,
    IReadOnlyCollection<AthletePerformancePassportDomainResponse> Dominios,
    IReadOnlyCollection<AthletePerformancePassportRecordResponse> MelhoresMarcas,
    string RegraDeUso)
{
    public IReadOnlyCollection<AthletePerformanceRecordResponse> Recordes { get; init; } =
        Array.Empty<AthletePerformanceRecordResponse>();

    public IReadOnlyCollection<AthleteTimedPerformanceResponse> Tempos { get; init; } =
        Array.Empty<AthleteTimedPerformanceResponse>();

    public IReadOnlyCollection<AthleteCompetitionTestResultResponse> Resultados { get; init; } =
        Array.Empty<AthleteCompetitionTestResultResponse>();

    public IReadOnlyCollection<AthleteSkillMilestoneResponse> HabilidadesMarcos { get; init; } =
        Array.Empty<AthleteSkillMilestoneResponse>();

    public AthletePerformanceEvolutionResponse Evolucao { get; init; } =
        new(180, Array.Empty<AthletePerformanceEvolutionPointResponse>(), 0, 0, 0, 0,
            "Sem base comparável suficiente para leitura longitudinal.");

    public ProgressIntelligenceFoundationResponse InteligenciaProgresso { get; init; } =
        new(180, Array.Empty<ProgressIntelligenceSignalResponse>(), 0, 0, 0,
            "Foundation descritiva sem score, ranking, diagnóstico ou recomendação automática.");

    public ProgressSignalContextSummaryResponse ContextoSinaisProgresso { get; init; } =
        new(180, Array.Empty<ProgressSignalContextResponse>(), 0, 0, 0,
            "Contextualização observacional sem score, ranking ou conclusão automática.");

    public MultiSignalTimelineResponse TimelineMultissinal { get; init; } =
        new(180, Array.Empty<MultiSignalTimelineEventResponse>(), 0, 0, 0,
            "Linha do tempo composta apenas por registros observados, sem interpolação ou previsão.");

    public ProgressEvidenceWindowsResponse JanelasEvidenciaProgresso { get; init; } =
        new(180, Array.Empty<ProgressEvidenceWindowItemResponse>(),
            "Janelas temporais descritivas sem score de confiança, prognóstico ou recomendação automática.");

    public CrossSignalObservationMapResponse MapaObservacaoCruzada { get; init; } =
        new(180, Array.Empty<CrossSignalObservationDayResponse>(), 0, 0,
            "Mapa de coobservação sem correlação, causalidade ou inferência automática.");

    public ProgressObservationSummaryResponse ResumoObservacionalProgresso { get; init; } =
        new(180, 0, 0, 0, 0, 0, 0, 0, 0,
            "SemCoberturaObservacional",
            "Resumo descritivo sem score, ranking, diagnóstico, prognóstico ou recomendação automática.");
}
