using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class InteresseExplorePaciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Intencao { get; set; } = "QueroExperimentar";
    public string Origem { get; set; } = "DeclaradoPeloAtleta";

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
}
