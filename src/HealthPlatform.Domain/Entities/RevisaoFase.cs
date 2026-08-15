using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RevisaoFase : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid RevisadoPorUsuarioId { get; set; }

    public string Dominio { get; set; } = string.Empty;
    public Guid FaseId { get; set; }
    public string FaseNome { get; set; } = string.Empty;
    public Guid? FaseDestinoId { get; set; }
    public string? FaseDestinoNome { get; set; }

    public string Decisao { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public DateTime DataUtc { get; set; } = DateTime.UtcNow;

    public string StatusAntes { get; set; } = string.Empty;
    public string StatusDepois { get; set; } = string.Empty;
    public int CriteriosConfigurados { get; set; }
    public int CriteriosAtendidos { get; set; }
    public bool ObjetivosProntosParaRevisao { get; set; }
    public bool OverrideCriterios { get; set; }
    public string? CriterioProfissional { get; set; }
    public string? SnapshotIndicadoresJson { get; set; }

    public Paciente Paciente { get; set; } = null!;
}
