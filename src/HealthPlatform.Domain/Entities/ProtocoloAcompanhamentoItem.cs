using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class ProtocoloAcompanhamentoItem : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public string Frequencia { get; set; } = "Diario";
    public string? HorarioLocal { get; set; }
    public string? DiasSemana { get; set; }
    public string? Instrucoes { get; set; }
    public bool Ativo { get; set; } = true;

    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
