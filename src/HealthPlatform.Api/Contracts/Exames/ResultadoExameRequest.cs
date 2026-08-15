using System.ComponentModel.DataAnnotations;

namespace HealthPlatform.Api.Contracts.Exames;

public class ResultadoExameRequest
{
    public Guid MarcadorId { get; set; }
    public decimal? ValorNumerico { get; set; }

    [MaxLength(300)]
    public string? ValorTexto { get; set; }

    [MaxLength(50)]
    public string? Unidade { get; set; }

    public decimal? ReferenciaMinima { get; set; }
    public decimal? ReferenciaMaxima { get; set; }

    [MaxLength(300)]
    public string? ReferenciaTexto { get; set; }

    public string? Observacao { get; set; }
}
