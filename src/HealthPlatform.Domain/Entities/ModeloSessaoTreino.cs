using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ModeloSessaoTreino : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }
    public string ConteudoJson { get; set; } = "{}";
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
