using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ItemRefeicaoPlano : BaseEntity
{
    public Guid RefeicaoPlanoAlimentarId { get; set; }
    public Guid AlimentoId { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "g";
    public decimal QuantidadeGramas { get; set; }
    public string? Observacao { get; set; }

    public RefeicaoPlanoAlimentar RefeicaoPlanoAlimentar { get; set; } = null!;
    public Alimento Alimento { get; set; } = null!;
    public ICollection<SubstituicaoItemRefeicao> Substituicoes { get; set; } = new List<SubstituicaoItemRefeicao>();
}
