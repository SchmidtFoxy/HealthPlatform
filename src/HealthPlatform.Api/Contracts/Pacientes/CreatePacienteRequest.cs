namespace HealthPlatform.Api.Contracts.Pacientes;

public sealed record CreatePacienteRequest(
    string Nome,
    string? Cpf,
    DateOnly? DataNascimento,
    string? Sexo,
    string? Telefone,
    string? Email,
    string? Profissao);
