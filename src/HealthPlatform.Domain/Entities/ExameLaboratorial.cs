using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ExameLaboratorial : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public DateTime DataColetaUtc { get; set; } = DateTime.UtcNow;
    public string? Laboratorio { get; set; }
    public string? Observacoes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public ICollection<ResultadoExameLaboratorial> Resultados { get; set; } = new List<ResultadoExameLaboratorial>();
}
