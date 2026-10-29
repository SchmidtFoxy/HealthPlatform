using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Alimento : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public decimal CaloriasPor100g { get; set; }
    public decimal ProteinasPor100g { get; set; }
    public decimal CarboidratosPor100g { get; set; }
    public decimal GordurasPor100g { get; set; }
    public decimal FibrasPor100g { get; set; }
    /// <summary>Medida caseira do alimento (ex.: "unidade", "fatia"). Usada com GramasPorUnidade.</summary>
    public string? UnidadeNome { get; set; }
    public decimal? GramasPorUnidade { get; set; }
    /// <summary>Densidade para prescrever em ml (g por ml). Sem valor, o app usa 1.</summary>
    public decimal? GramasPorMl { get; set; }
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public ICollection<ItemRefeicaoPlano> ItensRefeicao { get; set; } = new List<ItemRefeicaoPlano>();
    public ICollection<SubstituicaoItemRefeicao> Substituicoes { get; set; } = new List<SubstituicaoItemRefeicao>();
}
