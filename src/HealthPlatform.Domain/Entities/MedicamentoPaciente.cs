using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class MedicamentoPaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal? Dose { get; set; }
    public string? Unidade { get; set; }
    public string? Via { get; set; }
    public string Frequencia { get; set; } = "Diario";
    public string? HorariosLocais { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string? Orientacao { get; set; }
    public bool Ativo { get; set; } = true;

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public ICollection<RegistroMedicamento> Registros { get; set; } = new List<RegistroMedicamento>();
}
