namespace HealthPlatform.Api.Contracts.MarcadoresLaboratoriais;

public record MarcadorLaboratorialResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? UnidadePadrao,
    bool Ativo,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
