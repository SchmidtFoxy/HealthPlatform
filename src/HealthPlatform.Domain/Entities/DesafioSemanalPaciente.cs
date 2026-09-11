using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class DesafioSemanalPaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public DateOnly SemanaInicio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string TipoMetrica { get; set; } = string.Empty;
    public int Meta { get; set; }
    public int Progresso { get; set; }
    public int RecompensaXp { get; set; }
    public DateTime? ConcluidoEmUtc { get; set; }

    public Paciente Paciente { get; set; } = null!;
}
