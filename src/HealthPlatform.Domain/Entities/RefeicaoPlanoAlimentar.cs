using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RefeicaoPlanoAlimentar : BaseEntity
{
    public Guid PlanoAlimentarId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TimeOnly? Horario { get; set; }
    public int Ordem { get; set; }
    public string? Observacoes { get; set; }
    public decimal? MetaCalorias { get; set; }
    public decimal? MetaProteinasG { get; set; }
    public decimal? MetaCarboidratosG { get; set; }
    public decimal? MetaGordurasG { get; set; }
    public decimal? MetaFibrasG { get; set; }

    public PlanoAlimentar PlanoAlimentar { get; set; } = null!;
    public ICollection<ItemRefeicaoPlano> Itens { get; set; } = new List<ItemRefeicaoPlano>();
}
