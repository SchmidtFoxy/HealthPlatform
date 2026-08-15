using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RegistroMeta : BaseEntity
{
    public Guid MetaPacienteId { get; set; }
    public DateOnly Data { get; set; }
    public decimal? Valor { get; set; }
    public bool? Concluida { get; set; }
    public string? Observacao { get; set; }

    public MetaPaciente MetaPaciente { get; set; } = null!;
}
