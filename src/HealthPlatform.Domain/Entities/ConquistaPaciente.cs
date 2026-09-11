using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ConquistaPaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = "🏅";
    public DateOnly DataConquista { get; set; }
    public int RecompensaXp { get; set; }

    public Paciente Paciente { get; set; } = null!;
}
