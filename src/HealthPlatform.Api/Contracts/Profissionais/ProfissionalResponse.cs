namespace HealthPlatform.Api.Contracts.Profissionais;

public sealed record ProfissionalResponse(
    Guid Id,
    Guid UsuarioId,
    string Nome,
    string RegistroProfissional,
    string? Especialidade,
    string Tipo,
    bool Ativo,
    DateTime CreatedAtUtc);
