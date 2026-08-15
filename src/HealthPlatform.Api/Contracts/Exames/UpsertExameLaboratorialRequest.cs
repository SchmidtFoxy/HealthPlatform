using System.ComponentModel.DataAnnotations;

namespace HealthPlatform.Api.Contracts.Exames;

public class UpsertExameLaboratorialRequest
{
    public DateTime? DataColetaUtc { get; set; }

    [MaxLength(160)]
    public string? Laboratorio { get; set; }

    public string? Observacoes { get; set; }

    [Required]
    public List<ResultadoExameRequest> Resultados { get; set; } = new();
}
