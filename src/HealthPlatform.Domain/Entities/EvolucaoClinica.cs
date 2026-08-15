using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class EvolucaoClinica : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? ConsultaId { get; set; }
    public DateTime DataHoraUtc { get; set; } = DateTime.UtcNow;

    public string? Subjetivo { get; set; }
    public string? Objetivo { get; set; }
    public string? Avaliacao { get; set; }
    public string? Plano { get; set; }
    public string? Observacoes { get; set; }

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public Consulta? Consulta { get; set; }
}
