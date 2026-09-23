using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ProgramacaoNutricionalDia : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid PlanoAlimentarId { get; set; }
    public DateOnly Data { get; set; }
    public string Contexto { get; set; } = "Padrao";
    public string? Observacoes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public PlanoAlimentar PlanoAlimentar { get; set; } = null!;
}
