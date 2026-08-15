using HealthPlatform.Domain.Common;
using HealthPlatform.Domain.Enums;

namespace HealthPlatform.Domain.Entities;

public class Consulta : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public DateTime DataHoraUtc { get; set; }
    public string? Motivo { get; set; }
    public string? QueixaPrincipal { get; set; }
    public string? Evolucao { get; set; }
    public string? Conduta { get; set; }
    public string? Orientacoes { get; set; }
    public StatusConsulta Status { get; set; } = StatusConsulta.Agendada;

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public Avaliacao? Avaliacao { get; set; }
    public Anamnese? Anamnese { get; set; }
}
