using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class SuplementoPlanoAlimentar : BaseEntity
{
    public Guid PlanoAlimentarId { get; set; }
    public Guid SuplementoId { get; set; }
    public Guid? RefeicaoPlanoAlimentarId { get; set; }
    public decimal QuantidadePorcoes { get; set; } = 1m;
    public TimeOnly? Horario { get; set; }
    public string Contexto { get; set; } = "Outro";
    public string? Observacoes { get; set; }

    public PlanoAlimentar PlanoAlimentar { get; set; } = null!;
    public Suplemento Suplemento { get; set; } = null!;
    public RefeicaoPlanoAlimentar? RefeicaoPlanoAlimentar { get; set; }
}
