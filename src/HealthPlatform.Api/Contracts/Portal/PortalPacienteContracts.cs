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

public record PortalPacienteHomeResponse(
    DateOnly Data,
    PortalPacienteResumoResponse Paciente,
    PortalProximaConsultaResponse? ProximaConsulta,
    PortalProntidaoDiariaResponse? ProntidaoDiaria,
    PortalDorCorporalResumoResponse DorCorporal,
    PortalGamificacaoResponse Gamificacao,
    PortalCicloEsportivoResponse? CicloEsportivoAtual,
    PortalMetasCicloResponse MetasDoCiclo,
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
