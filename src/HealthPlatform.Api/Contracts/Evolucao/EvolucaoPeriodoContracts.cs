namespace HealthPlatform.Api.Contracts.Evolucao;

/// <summary>Totais de um periodo. Peso, gordura e cintura sao os da ultima avaliacao feita ate o fim do periodo.</summary>
public sealed record EvolucaoResumoPeriodoResponse(
    DateOnly Inicio,
    DateOnly Fim,
    int Treinos,
    int Minutos,
    int Series,
    decimal VolumeKg,
    decimal? EsforcoMedio,
    decimal? ProntidaoMedia,
    decimal? SonoMedio,
    int CheckIns,
    decimal? AdesaoDieta,
    int RefeicoesRegistradas,
    decimal? PesoKg,
    decimal? PercentualGordura,
    decimal? CinturaCm);

/// <summary>Uma semana (segunda a domingo) do periodo atual.</summary>
public sealed record EvolucaoSemanaResponse(
    DateOnly Inicio,
    int Treinos,
    int Minutos,
    int Series,
    decimal VolumeKg,
    decimal? EsforcoMedio,
    decimal? ProntidaoMedia,
    decimal? SonoMedio,
    int CheckIns,
    decimal? AdesaoDieta,
    int RefeicoesRegistradas);

public sealed record EvolucaoGrupoMuscularResponse(string Grupo, int SeriesAtual, int SeriesAnterior);

public sealed record EvolucaoAvaliacaoResponse(
    Guid Id,
    DateTime DataUtc,
    decimal? PesoKg,
    decimal? Imc,
    decimal? PercentualGordura,
    decimal? MassaMagraKg,
    decimal? MassaGordaKg,
    decimal? CinturaCm,
    decimal? AbdomenCm,
    decimal? QuadrilCm);

public sealed record EvolucaoFotoResponse(Guid Id, DateTime DataHoraUtc, string Pose, bool Anotada, string? Descricao, string Url);

public sealed record EvolucaoPeriodoResponse(
    int Dias,
    EvolucaoResumoPeriodoResponse Atual,
    EvolucaoResumoPeriodoResponse Anterior,
    IReadOnlyList<EvolucaoSemanaResponse> Semanas,
    IReadOnlyList<EvolucaoGrupoMuscularResponse> GruposMusculares,
    IReadOnlyList<EvolucaoAvaliacaoResponse> Avaliacoes,
    IReadOnlyList<EvolucaoFotoResponse> Fotos);
