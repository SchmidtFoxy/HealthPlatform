using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class SubstituicaoItemRefeicao : BaseEntity
{
    public Guid ItemRefeicaoPlanoId { get; set; }
    public Guid AlimentoId { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "g";
    public decimal QuantidadeGramas { get; set; }
    public string? Observacao { get; set; }

    public ItemRefeicaoPlano ItemRefeicaoPlano { get; set; } = null!;
    public Alimento Alimento { get; set; } = null!;
}
