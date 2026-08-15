namespace HealthPlatform.Api.Contracts.Dashboard;

public sealed record DashboardConsultaResumoResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime DataHoraUtc,
    DateTime DataHoraLocal,
    string Status,
    string? Motivo);

public sealed record DashboardPacienteAtencaoResponse(
    Guid PacienteId,
    string Nome,
    DateTime? UltimaConsultaUtc,
    DateTime? UltimoRegistroDiarioUtc,
    int DiasSemRegistroDiario,
    bool RetornoPendente);

public sealed record DashboardPacienteRecenteResponse(
    Guid PacienteId,
    string Nome,
    DateTime DataCadastroUtc,
    DateTime? UltimaConsultaUtc);

public sealed record DashboardProfissionalResponse(
    DateOnly Data,
    int OffsetMinutos,
    string ProfissionalNome,
    int PacientesAtivos,
    int PacientesAtendidosUltimos30Dias,
    int ConsultasHoje,
    int ConfirmadasHoje,
    int RealizadasHoje,
    int FaltasHoje,
    int RetornosPendentes,
    IReadOnlyCollection<DashboardConsultaResumoResponse> AgendaHoje,
    IReadOnlyCollection<DashboardConsultaResumoResponse> ProximasConsultas,
    IReadOnlyCollection<DashboardPacienteAtencaoResponse> PacientesQuePrecisamAtencao,
    IReadOnlyCollection<DashboardPacienteRecenteResponse> PacientesRecentes);
