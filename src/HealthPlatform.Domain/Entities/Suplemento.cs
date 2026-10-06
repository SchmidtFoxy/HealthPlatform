using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Suplemento : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string MarcaNormalizada { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Outros";
    public string? Forma { get; set; }
    public decimal PorcaoQuantidade { get; set; } = 1m;
    public string PorcaoUnidade { get; set; } = "porção";
    public decimal CaloriasPorPorcao { get; set; }
    public decimal ProteinasGPorPorcao { get; set; }
    public decimal CarboidratosGPorPorcao { get; set; }
    public decimal GordurasGPorPorcao { get; set; }
    public decimal FibrasGPorPorcao { get; set; }
    public decimal? CafeinaMgPorPorcao { get; set; }
    public string? Composicao { get; set; }
    public string? InstrucoesUso { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
}
