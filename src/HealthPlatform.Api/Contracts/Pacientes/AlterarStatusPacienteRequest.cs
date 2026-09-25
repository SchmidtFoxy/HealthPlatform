namespace HealthPlatform.Api.Contracts.Pacientes;

public sealed record AlterarStatusPacienteRequest(string Status, string? Motivo);
