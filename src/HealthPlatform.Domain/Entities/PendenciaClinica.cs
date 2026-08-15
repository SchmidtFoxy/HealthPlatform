using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class PendenciaClinica : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }

    public string? OrigemCodigo { get; set; }
    public string Categoria { get; set; } = "Acompanhamento";
    public string Severidade { get; set; } = "Media";
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ValorReferencia { get; set; }
    public string? AcaoSugerida { get; set; }

    public string Status { get; set; } = "Nova";
    public DateTime? VencimentoUtc { get; set; }
    public DateTime? VistaEmUtc { get; set; }
    public DateTime? AdiadaAteUtc { get; set; }
    public DateTime? ResolvidaEmUtc { get; set; }
    public string? Resolucao { get; set; }

    public Guid? ConsultaRetornoId { get; set; }

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public Consulta? ConsultaRetorno { get; set; }
}
