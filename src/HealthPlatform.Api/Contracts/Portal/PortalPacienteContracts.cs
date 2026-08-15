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

public record PortalPacienteHomeResponse(
    DateOnly Data,
    PortalPacienteResumoResponse Paciente,
    PortalProximaConsultaResponse? ProximaConsulta,
    PortalEvolucaoCorporalResponse EvolucaoCorporal,
    PortalPlanoAtualResponse? PlanoAlimentarAtual,
    IReadOnlyCollection<PortalMetaHojeResponse> MetasHoje,
    int MetasAtivas,
    int MetasConcluidas,
    decimal PercentualMetasConcluidas,
    IReadOnlyCollection<PortalRegistroDiarioResponse> RegistrosHoje,
    IReadOnlyCollection<PortalExameRecenteResponse> ExamesRecentes);
