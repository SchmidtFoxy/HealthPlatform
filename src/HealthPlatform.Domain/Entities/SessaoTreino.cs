using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class SessaoTreino : BaseEntity
{
    public Guid PlanoTreinoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? DiasSemana { get; set; }
    public int Ordem { get; set; }
    public string? Observacoes { get; set; }

    public PlanoTreino PlanoTreino { get; set; } = null!;
    public ICollection<ItemTreino> Itens { get; set; } = new List<ItemTreino>();
}
