using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RegistroMedicamento : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid MedicamentoId { get; set; }
    public DateTime DataHoraPrevistaUtc { get; set; }
    public DateTime? DataHoraTomadaUtc { get; set; }
    public string Status { get; set; } = "Tomado";
    public string? Observacao { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public MedicamentoPaciente Medicamento { get; set; } = null!;
}
