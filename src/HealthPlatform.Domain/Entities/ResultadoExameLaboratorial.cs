using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ResultadoExameLaboratorial : BaseEntity
{
    public Guid ExameLaboratorialId { get; set; }
    public Guid MarcadorLaboratorialId { get; set; }
    public decimal? ValorNumerico { get; set; }
    public string? ValorTexto { get; set; }
    public string? Unidade { get; set; }
    public decimal? ReferenciaMinima { get; set; }
    public decimal? ReferenciaMaxima { get; set; }
    public string? ReferenciaTexto { get; set; }
    public string? Observacao { get; set; }

    public ExameLaboratorial ExameLaboratorial { get; set; } = null!;
    public MarcadorLaboratorial MarcadorLaboratorial { get; set; } = null!;
}
