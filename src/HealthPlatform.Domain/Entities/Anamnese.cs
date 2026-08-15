using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Anamnese : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? ConsultaId { get; set; }
    public DateTime DataUtc { get; set; } = DateTime.UtcNow;

    public string? ObjetivoAcompanhamento { get; set; }
    public string? HistoricoDoencas { get; set; }
    public string? HistoricoFamiliar { get; set; }
    public string? Cirurgias { get; set; }
    public string? Alergias { get; set; }
    public string? Medicamentos { get; set; }
    public string? Suplementos { get; set; }

    public string? Tabagismo { get; set; }
    public string? Etilismo { get; set; }
    public decimal? SonoHorasMedia { get; set; }
    public string? SonoQualidade { get; set; }
    public bool? DespertaDuranteNoite { get; set; }
    public int? EstresseNivel { get; set; }

    public string? AtividadeFisica { get; set; }
    public int? AtividadeFisicaDiasSemana { get; set; }
    public string? HabitoIntestinal { get; set; }
    public decimal? AguaLitrosDia { get; set; }
    public string? Observacoes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public Consulta? Consulta { get; set; }
    public ICollection<RespostaAnamnesePersonalizada> RespostasPersonalizadas { get; set; } = new List<RespostaAnamnesePersonalizada>();
}
