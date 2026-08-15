namespace HealthPlatform.Api.Contracts.Agenda;

public sealed record AgendaConsultaResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime DataHoraUtc,
    DateTime DataHoraLocal,
    string Status,
    string? Motivo,
    string? Telefone,
    string? Email,
    bool PossuiAvaliacao,
    bool PossuiAnamnese);

public sealed record AgendaDiaResponse(
    DateOnly Data,
    int OffsetMinutos,
    int Total,
    int Agendadas,
    int Confirmadas,
    int Realizadas,
    int Canceladas,
    int Faltas,
    IReadOnlyCollection<AgendaConsultaResponse> Consultas);

public sealed record AlterarStatusAgendaRequest(string Status);
public sealed record ReagendarConsultaRequest(DateTime DataHoraLocal, int OffsetMinutos = 0);
