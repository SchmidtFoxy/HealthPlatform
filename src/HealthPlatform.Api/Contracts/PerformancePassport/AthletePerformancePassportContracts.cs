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





















































































































































public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse(
    Guid Id,
    Guid? TeamKnowledgeEffectDecisionRelacionadaId,
    Guid? TeamKnowledgeEffectReviewRelacionadaId,
    Guid? TeamKnowledgeEffectRelacionadoId,
    Guid? TeamKnowledgeApplicationRelacionadaId,
    Guid? TeamKnowledgeRelacionadoId,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalRevisor,
    string? Participantes,
    string RevisaoDocumentada,
    string? ContextoRevisao,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? ConclusaoDocumental,
    string? NecessidadeAcompanhamentoDocumentada,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionReviewStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionReviewFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionSummaryResponse(
    int Total,
    int Ativas,
    int Registradas,
    int EmRevisao,
    int Consolidadas,
    int Descartadas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionHistoryItemResponse(
    Guid Id,
    Guid TeamKnowledgeEffectDecisionId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionHistoryResponse(
    Guid TeamKnowledgeEffectDecisionId,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse(
    Guid Id,
    Guid? TeamKnowledgeEffectReviewRelacionadaId,
    Guid? TeamKnowledgeEffectRelacionadoId,
    Guid? TeamKnowledgeApplicationRelacionadaId,
    Guid? TeamKnowledgeRelacionadoId,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string DecisaoDocumentada,
    string? ContextoDecisao,
    string? BaseObservacionalEvidenciaSuporte,
    string? JustificativaProfissional,
    string? ResultadoEsperadoDocumentado,
    string? NecessidadeAcompanhamentoDocumentada,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeEffectDecisionFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewSummaryResponse(
    int Total,
    int Ativas,
    int Registradas,
    int EmRevisao,
    int Consolidadas,
    int Descartadas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectReviewProfissionalResumoResponse> PorProfissionalRevisor,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewFiltersResponse(
    string? Status,
    string? ProfissionalRevisor,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewHistoryItemResponse(
    Guid Id,
    Guid TeamKnowledgeEffectReviewId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewHistoryResponse(
    Guid TeamKnowledgeEffectReviewId,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectReviewHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse(
    Guid Id,
    Guid? TeamKnowledgeEffectRelacionadoId,
    Guid? TeamKnowledgeApplicationRelacionadaId,
    Guid? TeamKnowledgeRelacionadoId,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalRevisor,
    string? Participantes,
    string RevisaoDocumentada,
    string? ContextoRevisao,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? ConclusaoDocumental,
    string? NecessidadeAcompanhamentoDocumentada,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeEffectReviewFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamKnowledgeEffectSummaryResponse(
    int Total,
    int Ativos,
    int Registrados,
    int EmRevisao,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectHistoryItemResponse(
    Guid Id,
    Guid TeamKnowledgeEffectId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamKnowledgeEffectHistoryResponse(
    Guid TeamKnowledgeEffectId,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeEffectPersistedResponse(
    Guid Id,
    Guid? TeamKnowledgeApplicationRelacionadaId,
    Guid? TeamKnowledgeRelacionadoId,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string EfeitoObservadoDocumentado,
    string? ContextoObservacao,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? ResultadoObservadoDocumentado,
    string? ImpactoPercebidoDocumentado,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeEffectStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeEffectFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeEffectFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeApplicationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeApplicationProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamKnowledgeApplicationSummaryResponse(
    int Total,
    int Ativos,
    int Registrados,
    int EmRevisao,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeApplicationProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeApplicationFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeApplicationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeApplicationHistoryItemResponse(
    Guid Id,
    Guid TeamKnowledgeApplicationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamKnowledgeApplicationHistoryResponse(
    Guid TeamKnowledgeApplicationId,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeApplicationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeApplicationPersistedResponse(
    Guid Id,
    Guid? TeamKnowledgeRelacionadoId,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string AplicacaoDocumentada,
    string? ObjetivoAplicacao,
    string? ContextoAplicacao,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? ResultadoEsperadoDocumentado,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeApplicationStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeApplicationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeApplicationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamKnowledgeSummaryResponse(
    int Total,
    int Ativos,
    int Registrados,
    int EmRevisao,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgePersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgeHistoryItemResponse(
    Guid Id,
    Guid TeamKnowledgeId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamKnowledgeHistoryResponse(
    Guid TeamKnowledgeId,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamKnowledgePersistedResponse(
    Guid Id,
    Guid? TeamInsightRelacionadoId,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string ConhecimentoDocumentado,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? Aplicabilidade,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamKnowledgeStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamKnowledgeFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamKnowledgeFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamKnowledgeFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamInsightClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamInsightProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamInsightSummaryResponse(
    int Total,
    int Ativos,
    int Registrados,
    int EmRevisao,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamInsightProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamInsightFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamInsightPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamInsightHistoryItemResponse(
    Guid Id,
    Guid TeamInsightId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamInsightHistoryResponse(
    Guid TeamInsightId,
    IReadOnlyCollection<ProfessionalReviewTeamInsightHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamInsightPersistedResponse(
    Guid Id,
    Guid? TeamLearningRelacionadoId,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string InsightDocumentado,
    string? BaseObservacionalEvidenciaSuporte,
    string? InterpretacaoProfissional,
    string? Aplicabilidade,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamInsightStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamInsightFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamInsightFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamInsightFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamLearningClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamLearningProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamLearningSummaryResponse(
    int Total,
    int Ativos,
    int Registrados,
    int EmRevisao,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamLearningProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamLearningFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamLearningPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamLearningHistoryItemResponse(
    Guid Id,
    Guid TeamLearningId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamLearningHistoryResponse(
    Guid TeamLearningId,
    IReadOnlyCollection<ProfessionalReviewTeamLearningHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamLearningPersistedResponse(
    Guid Id,
    Guid? TeamOutcomeRelacionadoId,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string AprendizadoDocumentado,
    string? EvidenciaBaseObservacional,
    string? Aplicabilidade,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamLearningStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamLearningFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamLearningFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamLearningFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamOutcomeClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamOutcomeProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamOutcomeSummaryResponse(
    int Total,
    int Ativos,
    int Observados,
    int EmAcompanhamento,
    int Consolidados,
    int Descartados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamOutcomeProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamOutcomeFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamOutcomePersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamOutcomeHistoryItemResponse(
    Guid Id,
    Guid TeamOutcomeId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamOutcomeHistoryResponse(
    Guid TeamOutcomeId,
    IReadOnlyCollection<ProfessionalReviewTeamOutcomeHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamOutcomePersistedResponse(
    Guid Id,
    Guid? TeamDecisionRelacionadaId,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string ResultadoDocumentado,
    string? EvidenciaSuporte,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamOutcomeStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamOutcomeFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamOutcomeFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamOutcomeFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamDecisionClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamDecisionProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamDecisionSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewTeamDecisionProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamDecisionFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamDecisionPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamDecisionHistoryItemResponse(
    Guid Id,
    Guid TeamDecisionId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamDecisionHistoryResponse(
    Guid TeamDecisionId,
    IReadOnlyCollection<ProfessionalReviewTeamDecisionHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamDecisionPersistedResponse(
    Guid Id,
    Guid? TeamAlignmentRelacionadoId,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string DecisaoDocumentada,
    string? RacionalJustificativa,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamDecisionStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamDecisionFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamDecisionFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamDecisionFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamAlignmentClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamAlignmentProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewTeamAlignmentSummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewTeamAlignmentProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamAlignmentFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTeamAlignmentPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamAlignmentHistoryItemResponse(
    Guid Id,
    Guid TeamAlignmentId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTeamAlignmentHistoryResponse(
    Guid TeamAlignmentId,
    IReadOnlyCollection<ProfessionalReviewTeamAlignmentHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTeamAlignmentPersistedResponse(
    Guid Id,
    Guid? SharedContextRelacionadoId,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string? ObjetivoAlinhamento,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTeamAlignmentStatusRequest(
    string Status);

public sealed record ProfessionalReviewTeamAlignmentFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTeamAlignmentFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTeamAlignmentFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewSharedContextClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewSharedContextProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewSharedContextSummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewSharedContextProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewSharedContextFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivados,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewSharedContextPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewSharedContextHistoryItemResponse(
    Guid Id,
    Guid SharedContextId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewSharedContextHistoryResponse(
    Guid SharedContextId,
    IReadOnlyCollection<ProfessionalReviewSharedContextHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewSharedContextPersistedResponse(
    Guid Id,
    Guid? CollaborationRelacionadaId,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? Participantes,
    string? ContextoCompartilhado,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewSharedContextStatusRequest(
    string Status);

public sealed record ProfessionalReviewSharedContextFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewSharedContextFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewSharedContextFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewCollaborationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewCollaborationProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewCollaborationSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewCollaborationProfissionalResumoResponse> PorProfissionalResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewCollaborationFiltersResponse(
    string? Status,
    string? ProfissionalResponsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewCollaborationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewCollaborationHistoryItemResponse(
    Guid Id,
    Guid CollaborationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewCollaborationHistoryResponse(
    Guid CollaborationId,
    IReadOnlyCollection<ProfessionalReviewCollaborationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewCollaborationPersistedResponse(
    Guid Id,
    Guid? CoordinationRelacionadaId,
    Guid? EscalationRelacionadaId,
    Guid? ContinuityRelacionadaId,
    string ProfissionalResponsavel,
    string? ProfissionaisParticipantes,
    string? ContextoColaboracao,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewCollaborationStatusRequest(
    string Status);

public sealed record ProfessionalReviewCollaborationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewCollaborationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewCollaborationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewCoordinationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewCoordinationProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewCoordinationSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewCoordinationProfissionalResumoResponse> PorProfissionalCoordenador,
    string RegraDeUso);

public sealed record ProfessionalReviewCoordinationFiltersResponse(
    string? Status,
    string? ProfissionalCoordenador,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewCoordinationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewCoordinationHistoryItemResponse(
    Guid Id,
    Guid CoordinationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewCoordinationHistoryResponse(
    Guid CoordinationId,
    IReadOnlyCollection<ProfessionalReviewCoordinationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewCoordinationPersistedResponse(
    Guid Id,
    Guid? AssignmentRelacionadaId,
    Guid? DelegationRelacionadaId,
    Guid? HandoffRelacionadoId,
    Guid? ContinuityRelacionadaId,
    Guid? EscalationRelacionadaId,
    string ProfissionalCoordenador,
    string? ContextoCoordenacao,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewCoordinationStatusRequest(
    string Status);

public sealed record ProfessionalReviewCoordinationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewCoordinationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewCoordinationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewEscalationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewEscalationProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewEscalationSummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewEscalationProfissionalResumoResponse> PorOrigem,
    IReadOnlyCollection<ProfessionalReviewEscalationProfissionalResumoResponse> PorDestino,
    string RegraDeUso);

public sealed record ProfessionalReviewEscalationFiltersResponse(
    string? Status,
    string? ProfissionalOrigem,
    string? ProfissionalDestino,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewEscalationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewEscalationHistoryItemResponse(
    Guid Id,
    Guid EscalationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewEscalationHistoryResponse(
    Guid EscalationId,
    IReadOnlyCollection<ProfessionalReviewEscalationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewEscalationPersistedResponse(
    Guid Id,
    Guid? ContinuityRelacionadaId,
    Guid? HandoffRelacionadoId,
    Guid? DelegationRelacionadaId,
    Guid? AssignmentRelacionadaId,
    string ProfissionalOrigem,
    string ProfissionalDestino,
    string? ContextoEscalado,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewEscalationStatusRequest(
    string Status);

public sealed record ProfessionalReviewEscalationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewEscalationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewEscalationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewContinuityClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewContinuityProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewContinuitySummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewContinuityProfissionalResumoResponse> PorProfissionalSeguimento,
    string RegraDeUso);

public sealed record ProfessionalReviewContinuityFiltersResponse(
    string? Status,
    string? ProfissionalSeguimento,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewContinuityPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewContinuityHistoryItemResponse(
    Guid Id,
    Guid ContinuityId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewContinuityHistoryResponse(
    Guid ContinuityId,
    IReadOnlyCollection<ProfessionalReviewContinuityHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewContinuityPersistedResponse(
    Guid Id,
    Guid? HandoffRelacionadoId,
    Guid? DelegationRelacionadaId,
    Guid? AssignmentRelacionadaId,
    string ProfissionalSeguimento,
    string? ContextoContinuidade,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewContinuityStatusRequest(
    string Status);

public sealed record ProfessionalReviewContinuityFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewContinuityFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewContinuityFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewHandoffClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewHandoffProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewHandoffSummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProfessionalReviewHandoffProfissionalResumoResponse> PorOrigem,
    IReadOnlyCollection<ProfessionalReviewHandoffProfissionalResumoResponse> PorDestino,
    string RegraDeUso);

public sealed record ProfessionalReviewHandoffFiltersResponse(
    string? Status,
    string? ProfissionalOrigem,
    string? ProfissionalDestino,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewHandoffPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewHandoffHistoryItemResponse(
    Guid Id,
    Guid HandoffId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewHandoffHistoryResponse(
    Guid HandoffId,
    IReadOnlyCollection<ProfessionalReviewHandoffHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewHandoffPersistedResponse(
    Guid Id,
    Guid? DelegationRelacionadaId,
    Guid? AssignmentRelacionadaId,
    string ProfissionalOrigem,
    string ProfissionalDestino,
    string? ContextoTransferido,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewHandoffStatusRequest(
    string Status);

public sealed record ProfessionalReviewHandoffFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewHandoffFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewHandoffFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewDelegationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewDelegationProfissionalResumoResponse(
    string Profissional,
    int Total);

public sealed record ProfessionalReviewDelegationSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewDelegationProfissionalResumoResponse> PorDelegante,
    IReadOnlyCollection<ProfessionalReviewDelegationProfissionalResumoResponse> PorDelegado,
    string RegraDeUso);

public sealed record ProfessionalReviewDelegationFiltersResponse(
    string? Status,
    string? ProfissionalDelegante,
    string? ProfissionalDelegado,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewDelegationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewDelegationHistoryItemResponse(
    Guid Id,
    Guid DelegationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewDelegationHistoryResponse(
    Guid DelegationId,
    IReadOnlyCollection<ProfessionalReviewDelegationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewDelegationPersistedResponse(
    Guid Id,
    Guid? AssignmentRelacionadaId,
    string ProfissionalDelegante,
    string ProfissionalDelegado,
    string? ContextoDelegacao,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewDelegationStatusRequest(
    string Status);

public sealed record ProfessionalReviewDelegationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewDelegationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewDelegationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewAssignmentClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewAssignmentResponsavelResumoResponse(
    string ResponsavelPrincipal,
    int Total);

public sealed record ProfessionalReviewAssignmentSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewAssignmentResponsavelResumoResponse> PorResponsavelPrincipal,
    string RegraDeUso);

public sealed record ProfessionalReviewAssignmentFiltersResponse(
    string? Status,
    string? ResponsavelPrincipal,
    string? ApoioParticipante,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewAssignmentPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewAssignmentHistoryItemResponse(
    Guid Id,
    Guid AssignmentId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewAssignmentHistoryResponse(
    Guid AssignmentId,
    IReadOnlyCollection<ProfessionalReviewAssignmentHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewAssignmentPersistedResponse(
    Guid Id,
    Guid? TaskCoordinationRelacionadaId,
    string ResponsavelPrincipal,
    string? ApoioParticipante,
    string? ContextoAtribuicao,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewAssignmentStatusRequest(
    string Status);

public sealed record ProfessionalReviewAssignmentFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewAssignmentFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewAssignmentFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewTaskCoordinationClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewTaskCoordinationResponsavelResumoResponse(
    string Responsavel,
    int Total);

public sealed record ProfessionalReviewTaskCoordinationSummaryResponse(
    int Total,
    int Ativas,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewTaskCoordinationResponsavelResumoResponse> PorResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewTaskCoordinationFiltersResponse(
    string? Status,
    string? Responsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewTaskCoordinationPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewTaskCoordinationHistoryItemResponse(
    Guid Id,
    Guid TaskCoordinationId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewTaskCoordinationHistoryResponse(
    Guid TaskCoordinationId,
    IReadOnlyCollection<ProfessionalReviewTaskCoordinationHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewTaskCoordinationPersistedResponse(
    Guid Id,
    string TarefaOperacional,
    Guid? ActionPlanRelacionadoId,
    string? Responsavel,
    string? Horizonte,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewTaskCoordinationStatusRequest(
    string Status);

public sealed record ProfessionalReviewTaskCoordinationFieldResponse(
    string Chave,
    string Rotulo,
    bool Obrigatorio,
    string Tipo,
    string? Ajuda);

public sealed record ProfessionalReviewTaskCoordinationFoundationResponse(
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    IReadOnlyCollection<ProfessionalReviewTaskCoordinationFieldResponse> Campos,
    string RegraDeUso);

public sealed record ProfessionalReviewActionPlanClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProfessionalReviewActionPlanResponsavelResumoResponse(
    string Responsavel,
    int Total);

public sealed record ProfessionalReviewActionPlanSummaryResponse(
    int Total,
    int Ativos,
    int Planejadas,
    int EmAndamento,
    int Concluidas,
    int Canceladas,
    int Arquivadas,
    IReadOnlyCollection<ProfessionalReviewActionPlanResponsavelResumoResponse> PorResponsavel,
    string RegraDeUso);

public sealed record ProfessionalReviewActionPlanFiltersResponse(
    string? Status,
    string? Responsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProfessionalReviewActionPlanPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProfessionalReviewActionPlanHistoryItemResponse(
    Guid Id,
    Guid ActionPlanId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProfessionalReviewActionPlanHistoryResponse(
    Guid ActionPlanId,
    IReadOnlyCollection<ProfessionalReviewActionPlanHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProfessionalReviewActionPlanPersistedResponse(
    Guid Id,
    string AcaoOperacional,
    string? ObjetivoRelacionado,
    string? Responsavel,
    string? Horizonte,
    Guid? CarePlanRelacionadoId,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProfessionalReviewActionPlanStatusRequest(
    string Status);

public sealed record ProfessionalReviewActionPlanFieldResponse(
    string Chave,
    string Rotulo,
    string Descricao,
    bool Obrigatorio);

public sealed record ProfessionalReviewActionPlanFoundationResponse(
    IReadOnlyCollection<ProfessionalReviewActionPlanFieldResponse> Campos,
    int CamposDisponiveis,
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    string RegraDeUso);

public sealed record ProgressReviewCarePlanClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProgressReviewCarePlanResponsavelResumoResponse(
    string Responsavel,
    int Total);

public sealed record ProgressReviewCarePlanSummaryResponse(
    int Total,
    int Ativos,
    int Planejados,
    int EmAndamento,
    int Concluidos,
    int Cancelados,
    int Arquivados,
    IReadOnlyCollection<ProgressReviewCarePlanResponsavelResumoResponse> PorResponsavel,
    string RegraDeUso);

public sealed record ProgressReviewCarePlanFiltersResponse(
    string? Status,
    string? Responsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProgressReviewCarePlanPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProgressReviewCarePlanHistoryItemResponse(
    Guid Id,
    Guid CarePlanId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProgressReviewCarePlanHistoryResponse(
    Guid CarePlanId,
    IReadOnlyCollection<ProgressReviewCarePlanHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProgressReviewCarePlanPersistedResponse(
    Guid Id,
    string ObjetivoCuidado,
    string AcaoPlanejada,
    string? Responsavel,
    string? Horizonte,
    Guid? FollowUpRelacionadoId,
    string? ObservacaoProfissional,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProgressReviewCarePlanStatusRequest(
    string Status);

public sealed record ProgressReviewCarePlanFieldResponse(
    string Chave,
    string Rotulo,
    string Descricao,
    bool Obrigatorio);

public sealed record ProgressReviewCarePlanFoundationResponse(
    IReadOnlyCollection<ProgressReviewCarePlanFieldResponse> Campos,
    int CamposDisponiveis,
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    string RegraDeUso);

public sealed record ProgressReviewFollowUpClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProgressReviewFollowUpResponsavelResumoResponse(
    string Responsavel,
    int Total);

public sealed record ProgressReviewFollowUpSummaryResponse(
    int Total,
    int Ativos,
    int Abertos,
    int Revisados,
    int Encerrados,
    int Arquivados,
    IReadOnlyCollection<ProgressReviewFollowUpResponsavelResumoResponse> PorResponsavel,
    string RegraDeUso);

public sealed record ProgressReviewFollowUpFiltersResponse(
    string? Status,
    string? Responsavel,
    string? Horizonte,
    string? Texto,
    bool IncluirArquivadas,
    string Ordenacao,
    int Total,
    IReadOnlyCollection<ProgressReviewFollowUpPersistedResponse> Itens,
    string RegraDeUso);

public sealed record ProgressReviewFollowUpHistoryItemResponse(
    Guid Id,
    Guid FollowUpId,
    string Evento,
    string AutorNome,
    Guid? AutorUsuarioId,
    DateTime OcorridoEmUtc,
    string? Detalhes);

public sealed record ProgressReviewFollowUpHistoryResponse(
    Guid FollowUpId,
    IReadOnlyCollection<ProgressReviewFollowUpHistoryItemResponse> Itens,
    int Total,
    string Ordenacao,
    string RegraDeUso);

public sealed record ProgressReviewFollowUpPersistedResponse(
    Guid Id,
    string ItemAcompanhar,
    string? ContextoRelacionado,
    string? HorizonteRevisao,
    string? Responsavel,
    string? ObservacaoFollowUp,
    string Status,
    DateTime? StatusAtualizadoEmUtc,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc,
    bool Arquivada);

public sealed record ProgressReviewFollowUpStatusRequest(
    string Status);

public sealed record ProgressReviewFollowUpFieldResponse(
    string Chave,
    string Rotulo,
    string Descricao,
    bool Obrigatorio);

public sealed record ProgressReviewFollowUpFoundationResponse(
    IReadOnlyCollection<ProgressReviewFollowUpFieldResponse> Campos,
    int CamposDisponiveis,
    string EstadoPreparacao,
    bool PersistenciaDisponivel,
    string Escopo,
    string RegraDeUso);

public sealed record ProgressReviewContextClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

public sealed record ProgressReviewContextIntegrityResponse(
    bool Valido,
    string Estado,
    string? Tipo,
    string? Referencia,
    IReadOnlyCollection<string> Erros,
    string RegraDeUso);

public sealed record ProgressReviewContextNavigationResponse(
    string Tipo,
    string Rotulo,
    string Destino,
    string Seletor,
    string RegraDeUso);

public sealed record ProgressReviewContextOptionResponse(
    string Tipo,
    string Rotulo,
    string Descricao);

public sealed record ProgressReviewContextLinksResponse(
    IReadOnlyCollection<ProgressReviewContextOptionResponse> Opcoes,
    string RegraDeUso);

public sealed record ProgressReviewHistoryResponse(
    IReadOnlyCollection<ProgressReviewPersistedNoteResponse> Itens,
    int Total,
    string? Campo,
    Guid? AutorUsuarioId,
    DateTime? DeUtc,
    DateTime? AteUtc,
    bool IncluirArquivadas,
    string Ordenacao);

public sealed record ProgressReviewPersistedNoteResponse(
    Guid Id,
    string Campo,
    string Rotulo,
    string Conteudo,
    string? ContextoTipo,
    string? ContextoReferencia,
    string? NavegacaoDestino,
    Guid AutorUsuarioId,
    string AutorNome,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc);

public sealed record ProgressReviewNoteFieldResponse(
    string Chave,
    string Rotulo,
    string Categoria,
    string Descricao,
    bool Obrigatorio);

public sealed record ProgressReviewNotesFoundationResponse(
    int DiasObservados,
    string EstadoPreparacao,
    IReadOnlyCollection<ProgressReviewNoteFieldResponse> Campos,
    int CamposDisponiveis,
    bool PersistenciaDisponivel,
    string Escopo,
    string RegraDeUso);

public sealed record ProgressReviewWorkspaceSectionResponse(
    string Chave,
    string Titulo,
    bool Disponivel,
    int Itens,
    string Leitura);

public sealed record ProgressReviewWorkspaceResponse(
    int DiasObservados,
    string EstadoPreparacao,
    IReadOnlyCollection<ProgressReviewWorkspaceSectionResponse> Secoes,
    int SecoesDisponiveis,
    int SecoesEsperadas,
    string RegraDeUso);

public sealed record ProgressIntelligenceClosureResponse(
    int ComponentesEsperados,
    int ComponentesDisponiveis,
    IReadOnlyCollection<string> ComponentesPresentes,
    IReadOnlyCollection<string> ComponentesAusentes,
    string EstadoEstrutural,
    string RegraDeUso);

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

    public ProgressIntelligenceClosureResponse FechamentoInteligenciaProgresso { get; init; } =
        new(6, 0, Array.Empty<string>(), Array.Empty<string>(),
            "EstruturaObservacionalParcial",
            "Fechamento estrutural sem score clínico, prognóstico ou recomendação automática.");

    public ProgressReviewWorkspaceResponse WorkspaceRevisaoProgresso { get; init; } =
        new(180, "PreparacaoParcial", Array.Empty<ProgressReviewWorkspaceSectionResponse>(), 0, 6,
            "Workspace de revisão profissional sem decisão clínica automática.");

    public ProgressReviewNotesFoundationResponse FundacaoNotasRevisaoProgresso { get; init; } =
        new(180, "PreparacaoParcial", Array.Empty<ProgressReviewNoteFieldResponse>(), 0, false,
            "RevisaoProfissional",
            "Estrutura de notas separada dos dados observados e sem decisão clínica automática.");
}
