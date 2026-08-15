using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class PlanoAlimentar : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string Status { get; set; } = "Ativo";
    public string? Observacoes { get; set; }
    public Guid? PlanoOrigemId { get; set; }
    public int Versao { get; set; } = 1;
    public decimal AjustePercentual { get; set; }
    public decimal? MetaCalorias { get; set; }
    public decimal? MetaProteinasG { get; set; }
    public decimal? MetaCarboidratosG { get; set; }
    public decimal? MetaGordurasG { get; set; }
    public decimal? MetaFibrasG { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public PlanoAlimentar? PlanoOrigem { get; set; }
    public ICollection<PlanoAlimentar> VersoesDerivadas { get; set; } = new List<PlanoAlimentar>();
    public ICollection<RefeicaoPlanoAlimentar> Refeicoes { get; set; } = new List<RefeicaoPlanoAlimentar>();
    public ICollection<FaseNutricional> FasesNutricionais { get; set; } = new List<FaseNutricional>();
}
