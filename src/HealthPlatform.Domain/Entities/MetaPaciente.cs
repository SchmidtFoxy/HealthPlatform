using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class MetaPaciente : BaseEntity
{
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Habito";
    public decimal? ValorObjetivo { get; set; }
    public string? Unidade { get; set; }
    public string Frequencia { get; set; } = "Diaria";
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string Status { get; set; } = "Ativa";
    public string? Observacoes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public ICollection<RegistroMeta> Registros { get; set; } = new List<RegistroMeta>();
}
