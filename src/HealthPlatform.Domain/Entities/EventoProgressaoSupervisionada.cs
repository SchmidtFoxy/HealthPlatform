using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class EventoProgressaoSupervisionada : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? CicloEsportivoPacienteId { get; set; }
    public string Eixo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataAplicacaoUtc { get; set; }
    public string Status { get; set; } = "EmObservacao";
    public string? Observacoes { get; set; }
    public DateTime? EncerradoEmUtc { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public CicloEsportivoPaciente? CicloEsportivoPaciente { get; set; }
}
