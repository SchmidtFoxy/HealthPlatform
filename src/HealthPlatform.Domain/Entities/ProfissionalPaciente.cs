using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

/// <summary>
/// Equipe do paciente: cada profissional ligado aqui ve e atende o paciente. Quem nao esta
/// ligado nao enxerga o paciente nem os dados dele. O admin organiza essas ligacoes.
/// </summary>
public class ProfissionalPaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? CriadoPorUsuarioId { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
