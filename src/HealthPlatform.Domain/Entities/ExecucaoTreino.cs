using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ExecucaoTreino : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid PlanoTreinoId { get; set; }
    public Guid SessaoTreinoId { get; set; }
    public DateTime DataHoraInicioUtc { get; set; }
    public DateTime? DataHoraFimUtc { get; set; }
    public int? DuracaoMinutos { get; set; }
    public int? EsforcoPercebido { get; set; }
    public string? Observacoes { get; set; }
    public string Status { get; set; } = "Concluido";

    public Paciente Paciente { get; set; } = null!;
    public PlanoTreino PlanoTreino { get; set; } = null!;
    public SessaoTreino SessaoTreino { get; set; } = null!;
    public ICollection<ExecucaoItemTreino> Itens { get; set; } = new List<ExecucaoItemTreino>();
}
