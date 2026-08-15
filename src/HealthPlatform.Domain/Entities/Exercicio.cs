using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Exercicio : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? GrupoMuscular { get; set; }
    public string? Equipamento { get; set; }
    public string? Descricao { get; set; }
    public string? VideoUrl { get; set; }
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public ICollection<ItemTreino> ItensTreino { get; set; } = new List<ItemTreino>();
}
