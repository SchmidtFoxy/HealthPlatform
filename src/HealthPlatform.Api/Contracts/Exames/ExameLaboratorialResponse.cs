namespace HealthPlatform.Api.Contracts.Exames;

public record ExameLaboratorialResponse(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    string ProfissionalNome,
    DateTime DataColetaUtc,
    string? Laboratorio,
    string? Observacoes,
    IReadOnlyCollection<ResultadoExameResponse> Resultados,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
