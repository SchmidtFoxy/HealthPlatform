using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ExecucaoItemTreino : BaseEntity
{
    public Guid ExecucaoTreinoId { get; set; }
    public Guid ItemTreinoId { get; set; }
    public int? SeriesRealizadas { get; set; }
    public string? RepeticoesRealizadas { get; set; }
    public decimal? CargaRealizada { get; set; }
    public string? UnidadeCarga { get; set; }
    public int? EsforcoPercebido { get; set; }
    public bool Concluido { get; set; } = true;
    public string? Observacoes { get; set; }

    public ExecucaoTreino ExecucaoTreino { get; set; } = null!;
    public ItemTreino ItemTreino { get; set; } = null!;
}
