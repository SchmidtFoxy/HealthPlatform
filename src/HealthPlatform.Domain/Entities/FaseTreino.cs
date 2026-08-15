using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class FaseTreino : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Guid? PlanoTreinoId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Personalizada";
    public string? Objetivo { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int Ordem { get; set; } = 1;
    public string Status { get; set; } = "Planejada";
    public string? Observacoes { get; set; }
    public decimal? MetaPesoKg { get; set; }
    public int? MetaAdesaoPercentual { get; set; }
    public int? DuracaoMinimaDias { get; set; }
    public string? CriterioTransicao { get; set; }

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public PlanoTreino? PlanoTreino { get; set; }
}
