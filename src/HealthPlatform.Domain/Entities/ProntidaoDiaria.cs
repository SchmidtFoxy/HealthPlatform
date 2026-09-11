using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ProntidaoDiaria : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public DateOnly Data { get; set; }
    public decimal SonoHoras { get; set; }
    public int? SonoQualidade { get; set; }
    public int EnergiaNivel { get; set; }
    public int DorNivel { get; set; }
    public int DisposicaoNivel { get; set; }
    public int RecuperacaoNivel { get; set; }
    public decimal? HorasDesdeUltimoTreino { get; set; }
    public int? EsforcoUltimoTreino { get; set; }
    public int Score { get; set; }
    public string RecomendacaoTreino { get; set; } = "Normal";
    public string? MotivoRecomendacao { get; set; }
    public string Origem { get; set; } = "Paciente";

    public Paciente Paciente { get; set; } = null!;
}
