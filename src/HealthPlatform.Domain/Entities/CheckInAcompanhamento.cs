using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class CheckInAcompanhamento : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? FaseNutricionalId { get; set; }
    public Guid? FaseTreinoId { get; set; }
    public Guid RegistradoPorUsuarioId { get; set; }

    public DateTime DataUtc { get; set; } = DateTime.UtcNow;
    public decimal? PesoKg { get; set; }
    public int? AdesaoAlimentacaoPercentual { get; set; }
    public int? AdesaoTreinoPercentual { get; set; }
    public int? FomeNivel { get; set; }
    public int? EnergiaNivel { get; set; }
    public int? SonoNivel { get; set; }
    public int? PercepcaoEvolucaoNivel { get; set; }
    public string? Observacoes { get; set; }
    public string Origem { get; set; } = "Profissional";

    public Paciente Paciente { get; set; } = null!;
    public FaseNutricional? FaseNutricional { get; set; }
    public FaseTreino? FaseTreino { get; set; }
}
