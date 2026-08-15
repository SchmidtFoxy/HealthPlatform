using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RelatorioClinico : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public DateTime? DataInicioUtc { get; set; }
    public DateTime? DataFimUtc { get; set; }
    public DateTime DataGeracaoUtc { get; set; } = DateTime.UtcNow;
    public string Titulo { get; set; } = "Relatorio clinico";
    public string? ConclusaoMedica { get; set; }
    public string VersaoTemplate { get; set; } = "0.1.5";
    public string ConteudoJson { get; set; } = "{}";

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
