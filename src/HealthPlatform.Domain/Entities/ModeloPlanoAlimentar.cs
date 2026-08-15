using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ModeloPlanoAlimentar : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string ConteudoJson { get; set; } = "{}";
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
