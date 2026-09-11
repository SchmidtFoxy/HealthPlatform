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


public record PortalEstrategiaDiaResponse(
    string PerfilDia, string IntensidadeSugerida, int RpeMin, int RpeMax,
    int AjusteCargaPercentual, int AjusteVolumePercentual, string OrientacaoTreino,
    string EstrategiaNutricional, string FocoHidratacao, string Justificativa,
    IReadOnlyCollection<string> SessoesPrevistas, IReadOnlyCollection<string> RefeicoesChave);

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
    PortalGamificacaoResponse Gamificacao,
    PortalCicloEsportivoResponse? CicloEsportivoAtual,
    PortalEstrategiaDiaResponse EstrategiaDoDia,
    PortalEvolucaoCorporalResponse EvolucaoCorporal,
    PortalPlanoAtualResponse? PlanoAlimentarAtual,
    IReadOnlyCollection<PortalMetaHojeResponse> MetasHoje,
    int MetasAtivas,
    int MetasConcluidas,
    decimal PercentualMetasConcluidas,
    IReadOnlyCollection<PortalRegistroDiarioResponse> RegistrosHoje,
    IReadOnlyCollection<PortalExameRecenteResponse> ExamesRecentes);

public sealed record PortalJornadaItemResponse(string Tipo, Guid Id, DateTime DataUtc, string Titulo, string? Resumo, string? Complemento, string Destino);
