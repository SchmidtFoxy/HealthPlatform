using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class SolicitacaoClinica : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }

    public string Tipo { get; set; } = "Acompanhamento";
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime? DataLimiteUtc { get; set; }
    public string Status { get; set; } = "Pendente";

    public string? RespostaPaciente { get; set; }
    public string? LinkResposta { get; set; }
    public DateTime? RespondidaEmUtc { get; set; }
    public DateTime? RevisadaEmUtc { get; set; }
    public string? ObservacaoRevisao { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
