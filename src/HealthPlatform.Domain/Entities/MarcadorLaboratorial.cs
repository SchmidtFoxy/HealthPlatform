using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class MarcadorLaboratorial : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? UnidadePadrao { get; set; }
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public ICollection<ResultadoExameLaboratorial> Resultados { get; set; } = new List<ResultadoExameLaboratorial>();
}
