using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Avaliacao : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public DateTime DataUtc { get; set; } = DateTime.UtcNow;
    public decimal? PesoKg { get; set; }
    public decimal? AlturaM { get; set; }
    public decimal? PercentualGordura { get; set; }
    public decimal? MassaMagraKg { get; set; }
    public decimal? MassaGordaKg { get; set; }
    public decimal? CinturaCm { get; set; }
    public decimal? AbdomenCm { get; set; }
    public decimal? QuadrilCm { get; set; }
    public int? PressaoSistolica { get; set; }
    public int? PressaoDiastolica { get; set; }
    public int? FrequenciaCardiaca { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Consulta? Consulta { get; set; }
}
