namespace HealthPlatform.Api.Contracts.Dashboard;

public sealed record ProfessionalDailySignalItemResponse(
    Guid PacienteId,
    string PacienteNome,
    string Nivel,
    IReadOnlyCollection<string> Razoes,
    DateOnly? UltimoCheckInData,
    int? DiasSemCheckIn,
    int? ProntidaoScore,
    string? RecomendacaoTreino,
    string? MotivoRecomendacao,
    decimal? SonoHoras,
    int? EnergiaNivel,
    int? DorNivel,
    int? RecuperacaoNivel,
    DateOnly? UltimoFechamentoData,
    int? PercepcaoDoDia,
    int TreinosUltimos7Dias,
    DateTime? UltimoTreinoUtc);

public sealed record ProfessionalDailySignalsResponse(
    DateOnly DataReferencia,
    int JanelaDias,
    int TotalPacientesAtivos,
    int TotalComSinal,
    int RevisarHoje,
    int Observar,
    int ContextoPendente,
    IReadOnlyCollection<ProfessionalDailySignalItemResponse> Sinais,
    string RegraDeLeitura);
