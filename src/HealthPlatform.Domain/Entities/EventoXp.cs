using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class EventoXp : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public DateOnly Data { get; set; }
    public string Fonte { get; set; } = string.Empty;
    public Guid FonteId { get; set; }
    public int Pontos { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Adequacao { get; set; }

    public Paciente Paciente { get; set; } = null!;
}
