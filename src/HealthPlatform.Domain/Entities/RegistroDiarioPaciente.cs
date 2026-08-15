using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RegistroDiarioPaciente : BaseEntity
{
    public Guid PacienteId { get; set; }
    public DateTime DataHoraUtc { get; set; }
    public string Tipo { get; set; } = "Observacao";
    public string? Descricao { get; set; }
    public decimal? ValorNumerico { get; set; }
    public string? Unidade { get; set; }
    public int? Escala { get; set; }
    public string? ImagemUrl { get; set; }

    public Paciente Paciente { get; set; } = null!;
}
