using System.ComponentModel.DataAnnotations;

namespace HealthPlatform.Api.Contracts.MarcadoresLaboratoriais;

public class UpsertMarcadorLaboratorialRequest
{
    [Required, MaxLength(160)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Categoria { get; set; }

    [MaxLength(50)]
    public string? UnidadePadrao { get; set; }
}
