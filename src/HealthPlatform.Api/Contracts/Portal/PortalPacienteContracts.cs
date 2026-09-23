namespace HealthPlatform.Api.Contracts.Portal;

public record PortalPacienteResumoResponse(
    Guid Id,
    string Nome,
    DateOnly? DataNascimento,
    string? Sexo);

public record PortalProximaConsultaResponse(
    Guid Id,
    DateTime DataHoraUtc,
    string Status,
    string ProfissionalNome,
    string? Motivo);

public record PortalEvolucaoCorporalResponse(
    DateTime? DataUtc,
    decimal? PesoKg,
    decimal? PesoAnteriorKg,
    decimal? VariacaoPesoKg,
    decimal? Imc,
    decimal? PercentualGordura,
    decimal? CinturaCm);

public record PortalMetaHojeResponse(
    Guid Id,
    string Nome,
    string Tipo,
    decimal? ValorObjetivo,
    string? Unidade,
    decimal? ValorHoje,
    bool? Concluida,
    decimal? ProgressoPercentual);

public record PortalRegistroDiarioResponse(
    Guid Id,
    DateTime DataHoraUtc,
    string Tipo,
    string? Descricao,
    decimal? ValorNumerico,
    string? Unidade,
    int? Escala,
    string? ImagemUrl);

public record PortalRefeicaoResponse(Guid Id, string Nome, TimeOnly? Horario, int Ordem, int Itens);


public record PortalAdesaoNutricionalRefeicaoResponse(
    Guid RefeicaoId, string Nome, TimeOnly? Horario, string Status, string? Observacao);

public record PortalAdesaoNutricionalResponse(
    int RefeicoesPlanejadas, int RefeicoesRegistradas, int Realizadas, int Adaptadas, int NaoRealizadas,
    decimal? AdequacaoRegistradaPercentual, string Estado, string Mensagem,
    IReadOnlyCollection<PortalAdesaoNutricionalRefeicaoResponse> Refeicoes);

public record PortalHidratacaoContextualResponse(
    decimal? MetaMl, decimal? ConsumidoMl, decimal? ProgressoPercentual, int? TreinoMinutos, int? TreinoRpe,
    string Estado, string NivelAtencao, string Mensagem, IReadOnlyCollection<string> Sinais);

public sealed record PortalAlternativaRefeicaoResponse(
    Guid RefeicaoId, Guid PlanoAlimentarId, string PlanoNome, string Nome, TimeOnly? Horario,
    decimal Calorias, decimal ProteinasG, decimal CarboidratosG, decimal GordurasG, decimal FibrasG,
    decimal SimilaridadePercentual, IReadOnlyCollection<string> Itens);
public sealed record PortalAlternativasRefeicaoResponse(
    Guid RefeicaoOriginalId, string RefeicaoOriginalNome, decimal CaloriasOriginais,
    decimal ProteinasOriginaisG, decimal CarboidratosOriginaisG, decimal GordurasOriginaisG,
    IReadOnlyCollection<PortalAlternativaRefeicaoResponse> Alternativas);

public sealed record RegistrarAdesaoRefeicaoRequest(string Status, string? Observacao);

public record PortalPlanoAtualResponse(
    Guid Id,
    string Nome,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string ProfissionalNome,
    int Refeicoes,
    IReadOnlyCollection<PortalRefeicaoResponse> RotinaHoje);

public record PortalExameRecenteResponse(
    Guid ResultadoId,
    Guid ExameId,
    DateTime DataColetaUtc,
    string Marcador,
    decimal? ValorNumerico,
    string? ValorTexto,
    string? Unidade,
    string Classificacao);

public record PortalDorCorporalRegistroResponse(
    Guid Id, DateOnly Data, string Regiao, string? Lado, int Intensidade, int ImpactoTreino, string? Observacao);

public record PortalDorCorporalResumoResponse(
    int Registros7, int RegioesAtivas, int IntensidadeMaxima7, decimal? IntensidadeMedia7,
    int ImpactoMaximoTreino7, string NivelAtencao, string Mensagem,
    IReadOnlyCollection<PortalDorCorporalRegistroResponse> RegistrosRecentes);

public sealed record RegistrarDorCorporalRequest(
    DateOnly Data, string Regiao, string? Lado, int Intensidade, int ImpactoTreino, string? Observacao);


public record PortalMapaCorporalLongitudinalRegiaoResponse(
    string Regiao, string? Lado, int Registros56, int DiasComRegistro, int DiasRecentes28, int DiasAnteriores28,
    int IntensidadeMaxima, decimal? IntensidadeMedia, int ImpactoMaximoTreino, DateOnly PrimeiroRegistro, DateOnly UltimoRegistro,
    string Estado, string Tendencia, IReadOnlyCollection<DateOnly> OcorrenciasRecentes);

public record PortalMapaCorporalLongitudinalResponse(
    string Estado, string Titulo, string Resumo, int PeriodoDias, int TotalRegistros, int RegioesObservadas, int RegioesRecorrentes,
    IReadOnlyCollection<PortalMapaCorporalLongitudinalRegiaoResponse> Regioes, string MensagemSeguranca);



public record PortalRespostaSessaoEixoResponse(
    string Codigo, string Titulo, string Estado, string Valor, string Referencia, string Descricao);

public record PortalRespostaSessaoResponse(
    string Estado, string Titulo, string Resumo, DateTime? SessaoInicioUtc, string? SessaoNome, int? RpeSessao, int? DuracaoSessaoMinutos,
    DateOnly? DataCheckInResposta, int? ProntidaoResposta, int? RecuperacaoResposta, int? DorResposta, int? DisposicaoResposta,
    int RegistrosDorLocalizadaPosSessao, IReadOnlyCollection<PortalRespostaSessaoEixoResponse> Eixos, string Leitura, string MensagemSeguranca);

public record PortalComparacaoSessaoItemResponse(
    string Codigo, string Titulo, string Estado, string Planejado, string Executado, string Descricao);

public record PortalSessaoPlanejadaExecutadaResponse(
    string Estado, string Titulo, string Resumo, string? SessaoPlanejada, string? SessaoExecutada,
    int? RpeExecutado, int RpePlanejadoMin, int RpePlanejadoMax, int? DuracaoExecutadaMinutos,
    int SeriesPlanejadas, int SeriesExecutadas, decimal? VolumePlanejadoEstimado, decimal? VolumeExecutadoEstimado,
    string? MotivoAdaptacaoRegistrado, bool AdaptacaoCoerenteComContexto,
    IReadOnlyCollection<PortalComparacaoSessaoItemResponse> Comparacoes, string Leitura, string MensagemSeguranca);

public record PortalCriterioDisponibilidadeTreinoResponse(
    string Codigo, string Estado, string Titulo, string Valor, string Descricao);

public record PortalDisponibilidadeTreinoResponse(
    string Estado, string Titulo, string Resumo, string SessaoReferencia, bool ExigeAdaptacao,
    IReadOnlyCollection<PortalCriterioDisponibilidadeTreinoResponse> Criterios, string Acao, string MensagemSeguranca);

public record PortalProntidaoDiariaResponse(
    Guid Id,
    DateOnly Data,
    decimal SonoHoras,
    int? SonoQualidade,
    int EnergiaNivel,
    int DorNivel,
    int DisposicaoNivel,
    int RecuperacaoNivel,
    decimal? HorasDesdeUltimoTreino,
    int? EsforcoUltimoTreino,
    int Score,
    string RecomendacaoTreino,
    string? MotivoRecomendacao);

public sealed record RegistrarProntidaoDiariaRequest(
    DateOnly Data,
    decimal SonoHoras,
    int? SonoQualidade,
    int EnergiaNivel,
    int DorNivel,
    int DisposicaoNivel,
    int RecuperacaoNivel);

public record PortalCicloEsportivoResponse(
    Guid Id, string Nome, string PerfilEsportivo, string? Objetivo, DateOnly DataInicio, DateOnly DataFim,
    string Status, int SemanaAtual, int TotalSemanas, decimal ProgressoTemporalPercentual,
    int? MetaTreinosSemanais, int? MetaConsistenciaPercentual, decimal? MetaPesoKg,
    int TreinosNoCiclo, int CheckInsNoCiclo, decimal? MediaProntidao);

public record PortalMetaCicloItemResponse(
    string Codigo, string Titulo, string Estado, string ValorAtual, string Meta,
    decimal? ProgressoPercentual, string Descricao);

public record PortalMetasCicloResponse(
    string Estado, string Titulo, string Resumo,
    IReadOnlyCollection<PortalMetaCicloItemResponse> Itens, string MensagemSeguranca);

public record PortalCheckpointCicloItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Orientacao);

public record PortalCheckpointCicloResponse(
    string Estado, string Titulo, string Resumo, int SemanaAtual, int TotalSemanas, decimal ProgressoTemporalPercentual,
    IReadOnlyCollection<PortalCheckpointCicloItemResponse> Itens, string MensagemSeguranca);

public record PortalRelatorioCicloItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Leitura);

public record PortalRelatorioCicloResponse(
    string Estado, string Titulo, string Resumo, string Periodo, int SemanaAtual, int TotalSemanas,
    int TreinosNoCiclo, int CheckInsNoCiclo, decimal? MediaProntidao,
    IReadOnlyCollection<PortalRelatorioCicloItemResponse> Itens, string MensagemSeguranca);

public record PortalComparativoCicloItemResponse(
    Guid CicloId, string Nome, string PerfilEsportivo, string Status, DateOnly DataInicio, DateOnly DataFim,
    int DiasObservados, int Treinos, decimal TreinosPorSemana, int CheckIns, decimal CheckInsPorSemana,
    decimal? MediaProntidao, decimal? PesoInicialKg, decimal? PesoFinalKg, decimal? VariacaoPesoKg);

public record PortalComparativoCiclosResponse(
    string Estado, string Titulo, string Resumo, IReadOnlyCollection<PortalComparativoCicloItemResponse> Ciclos,
    string? ComparacaoComAnterior, string MensagemSeguranca);

public record PortalTendenciaObjetivoItemResponse(
    string Codigo, string Eixo, string Estado, string Titulo, string Evidencia, string Leitura);

public record PortalTendenciaObjetivoResponse(
    string Estado, string PerfilEsportivo, string? Objetivo, string Titulo, string Resumo,
    IReadOnlyCollection<PortalTendenciaObjetivoItemResponse> Itens, string MensagemSeguranca);

public record PortalAcaoPrioritariaCicloItemResponse(
    string Codigo, string Categoria, int Ordem, string Nivel, string Titulo, string Motivo, string Acao);

public record PortalAcoesPrioritariasCicloResponse(
    string Estado, string Titulo, string Resumo,
    IReadOnlyCollection<PortalAcaoPrioritariaCicloItemResponse> Prioridades, string MensagemSeguranca);

public record PortalPlanejamentoSemanalItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Orientacao);

public record PortalPlanejamentoSemanalResponse(
    string Estado, string Titulo, string Resumo, int? MetaTreinosSemana, int? TreinosConcluidosSemana, int? TreinosRestantesSemana,
    IReadOnlyCollection<PortalPlanejamentoSemanalItemResponse> Itens, string MensagemSeguranca);

public record PortalResumoSemanalItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia);

public record PortalResumoSemanalResponse(
    string Estado, string Titulo, string Resumo, DateOnly SemanaInicio, DateOnly SemanaFim,
    int? TreinosConcluidosSemana, int? MetaTreinosSemana, decimal ProgressoExecucaoHoje,
    IReadOnlyCollection<PortalResumoSemanalItemResponse> Itens, string MensagemSeguranca);

public record PortalTendenciaSemanalItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo,
    string ValorAtual, string ValorAnterior, string Variacao, string Leitura);

public record PortalTendenciaSemanalResponse(
    string Estado, string Titulo, string Resumo, DateOnly SemanaAtualInicio, DateOnly SemanaAtualFim,
    DateOnly SemanaAnteriorInicio, DateOnly SemanaAnteriorFim,
    IReadOnlyCollection<PortalTendenciaSemanalItemResponse> Itens, string MensagemSeguranca);

public record PortalRadarAdesaoItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Acao);

public record PortalRadarAdesaoResponse(
    string Estado, string Titulo, string Resumo, int SinaisAtencao, bool ContextoRecuperacao,
    IReadOnlyCollection<PortalRadarAdesaoItemResponse> Itens, string MensagemSeguranca);

public record PortalPlanoReconexaoItemResponse(
    string Codigo, string Categoria, int Ordem, string Titulo, string Motivo, string Acao);

public record PortalPlanoReconexaoResponse(
    string Estado, string Titulo, string Resumo, int Passos,
    IReadOnlyCollection<PortalPlanoReconexaoItemResponse> Itens, string MensagemSeguranca);

public record PortalProtecaoRetomadaItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Acao);

public record PortalProtecaoRetomadaResponse(
    string Estado, string Titulo, string Resumo, int SinaisFragilidade, bool RetomadaEmCurso,
    IReadOnlyCollection<PortalProtecaoRetomadaItemResponse> Itens, string MensagemSeguranca);

public record PortalEstabilidadeHabitoItemResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia, string Orientacao);

public record PortalEstabilidadeHabitosResponse(
    string Estado, string Titulo, string Resumo, int HabitosEstaveis, int HabitosConsolidando, int HabitosOscilando,
    IReadOnlyCollection<PortalEstabilidadeHabitoItemResponse> Itens, string MensagemSeguranca);

public record PortalProximoFocoHabitoResponse(
    string Estado, string Titulo, string Resumo, string? CodigoFoco, string? CategoriaFoco, string? EstadoDoFoco,
    string? Evidencia, string? Acao, IReadOnlyCollection<string> HabitosEmManutencao, string MensagemSeguranca);

public record PortalRevisaoFocoHabitoResponse(
    string Estado, string Titulo, string Resumo, string? CodigoFoco, string? CategoriaFoco, string? EstadoDoFoco,
    string Decisao, string Evidencia, string Acao, int HabitosEmManutencao, string MensagemSeguranca);

public record PortalEncerramentoCicloHabitoResponse(
    string Estado, string Titulo, string Resumo, string? CodigoHabito, string? CategoriaHabito,
    string Decisao, string Evidencia, string Acao, bool EmManutencao, int HabitosEmManutencao, string MensagemSeguranca);

public record PortalReentradaDesafioResponse(
    string Estado, string Titulo, string Resumo, bool ElegivelNovoDesafio, string? CategoriaBase,
    string Decisao, string Evidencia, string Acao, int HabitosEstaveis, int HabitosConsolidando, int HabitosOscilando,
    string MensagemSeguranca);

public record PortalCriterioJanelaProgressaoResponse(
    string Codigo, string Titulo, bool Atendido, string Estado, string Evidencia);

public record PortalJanelaProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool JanelaAberta, string Decisao,
    int CriteriosAtendidos, int CriteriosTotais, IReadOnlyCollection<PortalCriterioJanelaProgressaoResponse> Criterios,
    string Acao, string MensagemSeguranca);

public record PortalOpcaoDecisaoProgressaoResponse(
    string Codigo, string Categoria, string Titulo, string Prioridade, string Justificativa);

public record PortalDecisaoProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool PodeConsiderarProgressao, string? CategoriaSugerida,
    string Decisao, string Evidencia, string Acao, IReadOnlyCollection<PortalOpcaoDecisaoProgressaoResponse> Opcoes,
    string MensagemSeguranca);

public record PortalCriterioPlanoProgressaoResponse(
    string Codigo, string Titulo, string Estado, string Evidencia);

public record PortalPlanoProgressaoSupervisionadaResponse(
    string Estado, string Titulo, string Resumo, bool PodeAbrirDiscussao, string? EixoSupervisionado,
    string Decisao, string Evidencia, string Acao, IReadOnlyCollection<PortalCriterioPlanoProgressaoResponse> Criterios,
    string ProximaReavaliacao, string MensagemSeguranca);

public record PortalSinalRespostaProgressaoResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Evidencia);

public record PortalMonitoramentoRespostaProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool EmAcompanhamento, string? EixoSupervisionado,
    string Decisao, IReadOnlyCollection<PortalSinalRespostaProgressaoResponse> Sinais, string Acao,
    string MensagemSeguranca);

public record PortalEventoProgressaoSupervisionadaResponse(
    Guid Id, string Eixo, string Descricao, DateTime DataAplicacaoUtc, string Status,
    DateTime? EncerradoEmUtc, string? Observacoes, string ProfissionalNome, Guid? CicloEsportivoPacienteId);

public record PortalRegistroProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool PossuiEventoAtivo,
    PortalEventoProgressaoSupervisionadaResponse? EventoAtual, string MensagemSeguranca);

public record PortalComparativoProgressaoEixoResponse(
    string Codigo, string Titulo, string Unidade, decimal? Antes, decimal? Depois, decimal? Variacao,
    int RegistrosAntes, int RegistrosDepois, string Estado, string Evidencia);

public record PortalComparativoProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool PossuiMarcoTemporal, string? EixoProgressao,
    DateTime? DataAplicacaoUtc, int DiasJanelaAntes, int DiasJanelaDepois,
    IReadOnlyCollection<PortalComparativoProgressaoEixoResponse> Eixos, string Interpretacao, string MensagemSeguranca);

public record PortalInterpretacaoLongitudinalEixoResponse(
    string Codigo, string Titulo, string Estado, string Evidencia);

public record PortalInterpretacaoLongitudinalProgressaoResponse(
    string Estado, string Titulo, string Resumo, string? EixoProgressao, DateTime? DataAplicacaoUtc,
    bool JanelaComparavel, IReadOnlyCollection<PortalInterpretacaoLongitudinalEixoResponse> Eixos,
    string CondutaObservacional, string Interpretacao, string MensagemSeguranca);

public record PortalHistoricoProgressaoItemResponse(
    Guid Id, string Eixo, string Descricao, DateTime DataAplicacaoUtc, string Status,
    DateTime? EncerradoEmUtc, int DiasObservacao, string ProfissionalNome, Guid? CicloEsportivoPacienteId,
    string EstadoResposta, string ResumoResposta, string? Observacoes);

public record PortalHistoricoProgressaoResponse(
    string Estado, string Titulo, string Resumo, int TotalEventos, int EventosEncerrados, bool PossuiEmObservacao,
    IReadOnlyCollection<PortalHistoricoProgressaoItemResponse> Eventos, string Interpretacao, string MensagemSeguranca);

public record PortalToleranciaProgressaoEixoResponse(
    string Eixo, int TotalEventos, int EventosInterpretaveis, decimal? MediaDiasObservacao,
    string Padrao, string Leitura, IReadOnlyCollection<string> Evidencias);

public record PortalToleranciaProgressaoResponse(
    string Estado, string Titulo, string Resumo, int EixosAnalisados,
    IReadOnlyCollection<PortalToleranciaProgressaoEixoResponse> Eixos, string Interpretacao, string MensagemSeguranca);

public record PortalPainelMedicinaEsporteIndicadorResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Valor, string Contexto, string Prioridade);

public record PortalPainelMedicinaEsporteResponse(
    string Estado, string Titulo, string Resumo, int IndicadoresAtencao, int IndicadoresEstaveis,
    string PrioridadePrincipal, IReadOnlyCollection<PortalPainelMedicinaEsporteIndicadorResponse> Indicadores,
    IReadOnlyCollection<string> Contextos, string MensagemSeguranca);

public record PortalAlertaClinicoEsportivoResponse(
    string Codigo, string Nivel, string Titulo, string Resumo,
    IReadOnlyCollection<string> Sinais, IReadOnlyCollection<string> Origens, string Acao);

public record PortalAlertasClinicoEsportivosResponse(
    string Estado, string Titulo, string Resumo, int TotalAlertas, string PrioridadePrincipal,
    IReadOnlyCollection<PortalAlertaClinicoEsportivoResponse> Alertas, string MensagemSeguranca);

public record PortalPerfilRespostaAtletaEixoResponse(
    string Eixo, string PadraoHistorico, int TotalEventos, int EventosInterpretaveis,
    string EstadoAtual, string Contexto, IReadOnlyCollection<string> Evidencias);

public record PortalPerfilRespostaAtletaResponse(
    string Estado, string Titulo, string Resumo, int EixosComHistorico,
    int HabitosEstaveis, int HabitosOscilando, IReadOnlyCollection<PortalPerfilRespostaAtletaEixoResponse> Eixos,
    string LeituraGlobal, string MensagemSeguranca);

public record PortalComparacaoProgressaoEventoResponse(
    Guid Id, DateTime DataAplicacaoUtc, string Status, int DiasObservacao, string EstadoResposta, string ResumoResposta);

public record PortalComparacaoProgressaoEixoResponse(
    string Eixo, int TotalEventos, bool PossuiComparacao,
    IReadOnlyCollection<PortalComparacaoProgressaoEventoResponse> Eventos, string Leitura);

public record PortalComparacaoProgressaoResponse(
    string Estado, string Titulo, string Resumo, int EixosComHistorico, int EixosComparaveis,
    IReadOnlyCollection<PortalComparacaoProgressaoEixoResponse> Eixos, string Interpretacao, string MensagemSeguranca);

public record PortalCriterioReavaliacaoProgressaoResponse(
    string Codigo, string Titulo, string Estado, string Evidencia);

public record PortalReavaliacaoProgressaoResponse(
    string Estado, string Titulo, string Resumo, bool EmReavaliacao, bool PodeEncerrarObservacao,
    string? EixoSupervisionado, string Decisao, IReadOnlyCollection<PortalCriterioReavaliacaoProgressaoResponse> Criterios,
    string Acao, string ProximoPasso, string MensagemSeguranca);


public record PortalCargaIndividualizadaSemanaResponse(
    DateOnly Inicio, DateOnly Fim, int Sessoes, decimal CargaInterna, decimal? RpeMedio, int DuracaoMinutos);

public record PortalCargaIndividualizadaResponse(
    string Estado, string PosicaoHistorica, string Titulo, string Resumo, int DiasObservados,
    int SemanasHistoricas, int SemanasHistoricasAtivas, decimal? CargaAtual7, decimal? MedianaCargaHistorica,
    decimal? Quartil25Historico, decimal? Quartil75Historico, string ContextoRecuperacao, string ContextoRespostaSessao,
    IReadOnlyCollection<PortalCargaIndividualizadaSemanaResponse> Semanas, IReadOnlyCollection<string> Sinais,
    string Leitura, string MensagemSeguranca);


public record PortalBlocoTreinamentoResponse(
    string Estado, Guid? BlocoId, string? Nome, string? Tipo, string? Objetivo,
    DateOnly? DataInicio, DateOnly? DataFim, int? SemanaAtual, int? TotalSemanas, decimal? ProgressoTemporalPercentual,
    int? Ordem, string? Status, Guid? PlanoTreinoId, int TreinosExecutados, int DuracaoMinutosExecutada,
    decimal? RpeMedio, decimal? CargaInternaExecutada, string? CriterioTransicao, int? DuracaoMinimaDias,
    string PosicaoCargaAtual, string ContextoRespostaSessao, string Resumo, IReadOnlyCollection<string> Sinais, string MensagemSeguranca);

public record PortalDeloadRecuperacaoPlanejadaResponse(
    string Estado, bool IntencaoPlanejada, string TipoIntencao, string Titulo, string Resumo, string? BlocoNome,
    int? SemanaAtual, string PosicaoCargaAtual, string ContextoRespostaSessao, string ContextoAdesao,
    IReadOnlyCollection<string> Sinais, string MensagemSeguranca);

public record PortalCriterioReadinessContextualResponse(
    string Codigo, string Estado, string Titulo, string Evidencia);

public record PortalReadinessContextualTreinoResponse(
    string Estado, string Titulo, string Resumo, string SessaoPlanejada, string DemandaSessao,
    int? ProntidaoScore, string? RecomendacaoProntidao, bool CompativelComSessao, bool ExigeAdaptacao,
    IReadOnlyCollection<PortalCriterioReadinessContextualResponse> Criterios, string Acao, string MensagemSeguranca);


public record PortalRetornoGradualCriterioResponse(
    string Codigo, string Estado, string Titulo, string Evidencia);

public record PortalRetornoGradualResponse(
    string Estado, string Titulo, string Resumo, string MotivoContexto, int? DiasPausaDetectada,
    DateTime? UltimaSessaoUtc, DateTime? SessaoRetornoUtc, bool DorRecente, bool PodeAvancarEtapa,
    string ContextoReadiness, string ContextoRespostaSessao, string ContextoCarga,
    IReadOnlyCollection<PortalRetornoGradualCriterioResponse> Criterios, string Acao, string MensagemSeguranca);


public record PortalRelatorioEsportivoSecaoResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Resumo,
    IReadOnlyCollection<string> Evidencias);

public record PortalRelatorioEsportivoProfissionalResponse(
    string Estado, string Titulo, DateOnly DataReferencia, string Periodo, string ResumoExecutivo,
    string PrioridadePrincipal, string? CicloNome, string? ObjetivoCiclo,
    IReadOnlyCollection<PortalRelatorioEsportivoSecaoResponse> Secoes,
    IReadOnlyCollection<string> PontosAtencao, IReadOnlyCollection<string> PontosEstaveis,
    string MensagemSeguranca);

public record PortalEstrategiaDiaResponse(
    string PerfilDia, string IntensidadeSugerida, int RpeMin, int RpeMax,
    int AjusteCargaPercentual, int AjusteVolumePercentual, string OrientacaoTreino,
    string EstrategiaNutricional, string FocoHidratacao, string Justificativa,
    IReadOnlyCollection<string> SessoesPrevistas, IReadOnlyCollection<string> RefeicoesChave);



public record PortalSinalRecuperacaoResponse(string Codigo, string Severidade, string Titulo, string Descricao);

public record PortalTendenciaRecuperacaoResponse(
    int DiasObservados, decimal? ProntidaoMedia7, decimal? ProntidaoMediaAnterior7, decimal? VariacaoProntidao,
    decimal? SonoMedio7, decimal? DorMedia7, decimal? RecuperacaoMedia7, decimal? EnergiaMedia7,
    int Treinos7, int TreinosIntensos7, string Tendencia, string NivelAtencao, string Mensagem,
    IReadOnlyCollection<PortalSinalRecuperacaoResponse> Sinais);

public record PortalCargaTreinoResponse(
    int DiasObservados, int Treinos7, int TreinosIntensos7, int? DuracaoMinutos7,
    decimal? CargaInterna7, decimal? CargaMediaSemanalBase, decimal? RelacaoCargaComBase,
    decimal? RpeMedio7, int? DiasDesdeUltimoTreino, string Classificacao, string NivelAtencao,
    string Mensagem, IReadOnlyCollection<string> Sinais);

public record PortalPerformanceExercicioResponse(
    Guid ExercicioId, string Exercicio, string? GrupoMuscular, decimal? MelhorCarga, string? UnidadeCarga,
    DateTime? DataMelhorCarga, decimal? UltimaCarga, decimal? EvolucaoMelhorCargaPercentual,
    decimal? MelhorVolumeEstimado, bool NovoPrRecente, int? ProntidaoNoPr, int Registros);

public record PortalPerformanceResponse(
    int DiasObservados, int TreinosPeriodo, int ExerciciosAcompanhados, int PrsRecentes,
    decimal? VolumeEstimado28, decimal? VolumeEstimadoAnterior28, decimal? VariacaoVolumePercentual,
    string Tendencia, string Mensagem, IReadOnlyCollection<PortalPerformanceExercicioResponse> Destaques);



public record PortalEvolucaoEsportivaIndicadorResponse(
    string Codigo, string Categoria, string Estado, string Titulo, string Valor, string? Referencia, string Tendencia, string Descricao);

public record PortalEvolucaoEsportivaResponse(
    string Estado, string Titulo, string Resumo, int IndicadoresFavoraveis, int IndicadoresAtencao,
    IReadOnlyCollection<PortalEvolucaoEsportivaIndicadorResponse> Indicadores, string MensagemSeguranca);

public record PortalPlanoRecuperacaoItemResponse(
    string Codigo, string Categoria, string Prioridade, string Titulo, string Orientacao);

public record PortalPlanoRecuperacaoResponse(
    string Estado, string FocoPrincipal, string Resumo,
    IReadOnlyCollection<PortalPlanoRecuperacaoItemResponse> Itens, string MensagemSeguranca);

public record PortalCoachPrioridadeResponse(
    string Codigo, string Categoria, string Nivel, string Titulo, string Motivo, string Acao);

public record PortalCoachDiarioResponse(
    string Estado, string Titulo, string Resumo, string MensagemSeguranca,
    IReadOnlyCollection<PortalCoachPrioridadeResponse> Prioridades);

public record PortalExecucaoDiaItemResponse(
    string Codigo, string Categoria, string Titulo, string Descricao, string Status,
    bool Obrigatorio, decimal ProgressoPercentual, string? ValorAtual, string? Meta, string Acao);

public record PortalExecucaoDiaResponse(
    int TotalItens, int Concluidos, int Pendentes, decimal ProgressoPercentual,
    bool DiaFechado, int? PercepcaoDoDia, string? ResumoFechamento,
    IReadOnlyCollection<PortalExecucaoDiaItemResponse> Itens);

public sealed record FecharDiaRequest(int PercepcaoDoDia, string? Resumo);

public record PortalEventoXpResponse(Guid Id, DateOnly Data, string Fonte, int Pontos, string Motivo, string? Adequacao);
public record PortalDesafioSemanalResponse(Guid Id, string Codigo, string Titulo, string Descricao, int Meta, int Progresso, int RecompensaXp, bool Concluido);
public record PortalConquistaResponse(Guid Id, string Codigo, string Titulo, string Descricao, string Icone, DateOnly DataConquista, int RecompensaXp);

public record PortalGamificacaoResponse(
    int XpTotal, int Nivel, int XpNoNivel, int XpProximoNivel, int ConsistenciaScore,
    int StreakDias, int XpHoje, int DiasAtivos14, IReadOnlyCollection<PortalEventoXpResponse> EventosRecentes,
    IReadOnlyCollection<PortalDesafioSemanalResponse> DesafiosSemana, IReadOnlyCollection<PortalConquistaResponse> ConquistasRecentes);

public record PortalProgressoPositivoDestaqueResponse(string Fonte, int Xp, int Dias);
public record PortalProgressoPositivoResponse(
    string Estado, string TendenciaSemanal, int XpTotal, int Nivel, int XpSemana, int XpSemanaAnterior,
    int DiasComProgresso7, int StreakDias, int ConsistenciaScore, decimal ProgressoNivelPercentual, int XpAteProximoNivel,
    string Resumo, IReadOnlyCollection<PortalProgressoPositivoDestaqueResponse> Destaques, string MensagemSeguranca);

public record PortalGamificacao2SinalResponse(string Codigo, string Categoria, string Estado, string Titulo, string Descricao);
public record PortalGamificacao2Response(
    string Estado, string Titulo, string FocoAtual, string Mensagem, int XpTotal, int Nivel, int ConsistenciaScore, int StreakDias,
    int EventosAlinhadosRecentes, int EventosExcessoRecentes, IReadOnlyCollection<PortalGamificacao2SinalResponse> Sinais, string MensagemSeguranca);

public record PortalMissaoContextual2ItemResponse(
    Guid Id, string Codigo, string Titulo, string Descricao, int Meta, int Progresso, int RecompensaXp, bool Concluido,
    string EstadoContextual, string OrientacaoContextual);
public record PortalMissoesContextuais2Response(
    string Estado, string Titulo, string FocoAtual, string Mensagem, int TotalMissoes, int MissoesConcluidas, int MissoesSemPressaoHoje,
    IReadOnlyCollection<PortalMissaoContextual2ItemResponse> Missoes, string MensagemSeguranca);

public record PortalFocoGamificadoDiaResponse(
    string Estado, string Titulo, string Categoria, string Foco, string Orientacao,
    string? MissaoCodigo, string? MissaoTitulo, int Progresso, int Meta, int RecompensaXp, bool ProtegidoHoje,
    string Contexto, string MensagemSeguranca);

public record PortalPacienteHomeResponse(
    DateOnly Data,
    PortalPacienteResumoResponse Paciente,
    PortalProximaConsultaResponse? ProximaConsulta,
    PortalProntidaoDiariaResponse? ProntidaoDiaria,
    PortalDorCorporalResumoResponse DorCorporal,
    PortalGamificacaoResponse Gamificacao,
    PortalGamificacao2Response Gamificacao2,
    PortalMissoesContextuais2Response MissoesContextuais2,
    PortalFocoGamificadoDiaResponse FocoGamificadoDoDia,
    PortalProgressoPositivoResponse ProgressoPositivo,
    PortalCicloEsportivoResponse? CicloEsportivoAtual,
    PortalMetasCicloResponse MetasDoCiclo,
    PortalCheckpointCicloResponse CheckpointDoCiclo,
    PortalRelatorioCicloResponse RelatorioDoCiclo,
    PortalComparativoCiclosResponse ComparativoDeCiclos,
    PortalTendenciaObjetivoResponse TendenciaDoObjetivo,
    PortalAcoesPrioritariasCicloResponse AcoesPrioritariasDoCiclo,
    PortalPlanejamentoSemanalResponse PlanejamentoSemanal,
    PortalResumoSemanalResponse ResumoSemanal,
    PortalTendenciaSemanalResponse TendenciaSemanal,
    PortalRadarAdesaoResponse RadarAdesao,
    PortalPlanoReconexaoResponse PlanoReconexao,
    PortalProtecaoRetomadaResponse ProtecaoRetomada,
    PortalEstabilidadeHabitosResponse EstabilidadeHabitos,
    PortalProximoFocoHabitoResponse ProximoFocoHabito,
    PortalRevisaoFocoHabitoResponse RevisaoFocoHabito,
    PortalEncerramentoCicloHabitoResponse EncerramentoCicloHabito,
    PortalReentradaDesafioResponse ReentradaDesafio,
    PortalJanelaProgressaoResponse JanelaProgressao,
    PortalDecisaoProgressaoResponse DecisaoProgressao,
    PortalPlanoProgressaoSupervisionadaResponse PlanoProgressaoSupervisionada,
    PortalMonitoramentoRespostaProgressaoResponse MonitoramentoRespostaProgressao,
    PortalReavaliacaoProgressaoResponse ReavaliacaoProgressao,
    PortalRegistroProgressaoResponse RegistroProgressao,
    PortalComparativoProgressaoResponse ComparativoProgressao,
    PortalInterpretacaoLongitudinalProgressaoResponse InterpretacaoLongitudinalProgressao,
    PortalHistoricoProgressaoResponse HistoricoProgressoes,
    PortalComparacaoProgressaoResponse ComparacaoProgressoes,
    PortalToleranciaProgressaoResponse ToleranciaProgressao,
    PortalPerfilRespostaAtletaResponse PerfilRespostaAtleta,
    PortalPainelMedicinaEsporteResponse PainelMedicinaEsporte,
    PortalAlertasClinicoEsportivosResponse AlertasClinicoEsportivos,
    PortalMapaCorporalLongitudinalResponse MapaCorporalLongitudinal,
    PortalDisponibilidadeTreinoResponse DisponibilidadeTreino,
    PortalSessaoPlanejadaExecutadaResponse SessaoPlanejadaExecutada,
    PortalRespostaSessaoResponse RespostaSessao,
    PortalCargaIndividualizadaResponse CargaIndividualizada,
    PortalBlocoTreinamentoResponse BlocoTreinamento,
    PortalDeloadRecuperacaoPlanejadaResponse DeloadRecuperacaoPlanejada,
    PortalReadinessContextualTreinoResponse ReadinessContextualTreino,
    PortalRetornoGradualResponse RetornoGradual,
    PortalRelatorioEsportivoProfissionalResponse RelatorioEsportivoProfissional,
    PortalEstrategiaDiaResponse EstrategiaDoDia,
    PortalTendenciaRecuperacaoResponse TendenciaRecuperacao,
    PortalCargaTreinoResponse CargaTreino,
    PortalPerformanceResponse Performance,
    PortalEvolucaoEsportivaResponse EvolucaoEsportiva,
    PortalPlanoRecuperacaoResponse PlanoRecuperacao,
    PortalAdesaoNutricionalResponse AdesaoNutricional,
    PortalHidratacaoContextualResponse HidratacaoContextual,
    PortalCoachDiarioResponse CoachDiario,
    PortalExecucaoDiaResponse ExecucaoDoDia,
    PortalEvolucaoCorporalResponse EvolucaoCorporal,
    PortalPlanoAtualResponse? PlanoAlimentarAtual,
    IReadOnlyCollection<PortalMetaHojeResponse> MetasHoje,
    int MetasAtivas,
    int MetasConcluidas,
    decimal PercentualMetasConcluidas,
    IReadOnlyCollection<PortalRegistroDiarioResponse> RegistrosHoje,
    IReadOnlyCollection<PortalExameRecenteResponse> ExamesRecentes);

public sealed record PortalJornadaItemResponse(string Tipo, Guid Id, DateTime DataUtc, string Titulo, string? Resumo, string? Complemento, string Destino);
