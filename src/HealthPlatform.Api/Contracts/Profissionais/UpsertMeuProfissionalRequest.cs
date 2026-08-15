namespace HealthPlatform.Api.Contracts.Profissionais;

public sealed record UpsertMeuProfissionalRequest(
    string Nome,
    string RegistroProfissional,
    string? Especialidade);
