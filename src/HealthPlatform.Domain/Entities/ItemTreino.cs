using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ItemTreino : BaseEntity
{
    public Guid SessaoTreinoId { get; set; }
    public Guid ExercicioId { get; set; }
    public int Ordem { get; set; }
    public int Series { get; set; }
    public string Repeticoes { get; set; } = string.Empty;
    public decimal? Carga { get; set; }
    public string? UnidadeCarga { get; set; }
    public int? DescansoSegundos { get; set; }
    public int? TempoSegundos { get; set; }
    public string? Observacoes { get; set; }

    public SessaoTreino SessaoTreino { get; set; } = null!;
    public Exercicio Exercicio { get; set; } = null!;
    public ICollection<ExecucaoItemTreino> Execucoes { get; set; } = new List<ExecucaoItemTreino>();
}
