namespace HealthPlatform.Api.Contracts.BibliotecaMovimento;

public sealed record BibliotecaMovimentoExercicioResponse(
    Guid Id,
    string Nome,
    string? GrupoMuscular,
    string? Equipamento,
    string? Descricao,
    string? VideoUrl);

public sealed record BibliotecaMovimentoCapacidadeResponse(
    string Codigo,
    string Nome,
    IReadOnlyCollection<BibliotecaMovimentoExercicioResponse> Exercicios);

public sealed record BibliotecaMovimentoObjetivoResponse(
    string Codigo,
    string Nome,
    IReadOnlyCollection<BibliotecaMovimentoCapacidadeResponse> Capacidades);

public sealed record BibliotecaMovimentoModalidadeResponse(
    string Codigo,
    string Nome,
    string Descricao,
    IReadOnlyCollection<string> Ambientes,
    IReadOnlyCollection<string> EquipamentosComuns,
    IReadOnlyCollection<BibliotecaMovimentoObjetivoResponse> Objetivos);

public sealed record BibliotecaMovimentoResponse(
    string Estrutura,
    int TotalModalidades,
    int TotalExerciciosAtivos,
    IReadOnlyCollection<BibliotecaMovimentoModalidadeResponse> Modalidades,
    string FonteDosExercicios,
    string RegraDeSeguranca);
