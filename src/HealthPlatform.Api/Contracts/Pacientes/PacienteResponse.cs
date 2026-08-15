namespace HealthPlatform.Api.Contracts.Pacientes;

public sealed record PacienteResponse(
    Guid Id,
    string Nome,
    string? Cpf,
    DateOnly? DataNascimento,
    string? Sexo,
    string? Telefone,
    string? Email,
    string? Profissao,
    bool Ativo,
    DateTime CreatedAtUtc);
