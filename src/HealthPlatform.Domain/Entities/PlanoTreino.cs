using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class PlanoTreino : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string Status { get; set; } = "Ativo";
    public string? Observacoes { get; set; }
    public Guid? PlanoOrigemId { get; set; }
    public int Versao { get; set; } = 1;
    public decimal AjusteCargaPercentual { get; set; }
    public int AjusteSeries { get; set; }
    public int AjusteRepeticoes { get; set; }
    public int AjusteDescansoSegundos { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public PlanoTreino? PlanoOrigem { get; set; }
    public ICollection<PlanoTreino> VersoesDerivadas { get; set; } = new List<PlanoTreino>();
    public ICollection<SessaoTreino> Sessoes { get; set; } = new List<SessaoTreino>();
    public ICollection<FaseTreino> FasesTreino { get; set; } = new List<FaseTreino>();
}
