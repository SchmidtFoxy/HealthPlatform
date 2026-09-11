using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class CicloEsportivoPaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? FaseTreinoId { get; set; }
    public Guid? FaseNutricionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string PerfilEsportivo { get; set; } = "QualidadeDeVida";
    public string? Objetivo { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public string Status { get; set; } = "Planejado";
    public int? MetaTreinosSemanais { get; set; }
    public int? MetaConsistenciaPercentual { get; set; }
    public decimal? MetaPesoKg { get; set; }
    public string? Observacoes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public FaseTreino? FaseTreino { get; set; }
    public FaseNutricional? FaseNutricional { get; set; }
}
