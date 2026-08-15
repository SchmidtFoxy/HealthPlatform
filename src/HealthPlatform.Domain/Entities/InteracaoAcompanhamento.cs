using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class InteracaoAcompanhamento : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }

    public DateTime DataHoraUtc { get; set; }
    public string Canal { get; set; } = "Outro";
    public string Resultado { get; set; } = "Contato realizado";
    public string? Observacoes { get; set; }
    public DateTime? ProximoContatoUtc { get; set; }

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
