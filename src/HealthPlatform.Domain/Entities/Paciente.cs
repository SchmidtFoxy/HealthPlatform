using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Paciente : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Profissao { get; set; }
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
    public ICollection<Anamnese> Anamneses { get; set; } = new List<Anamnese>();
    public ICollection<ExameLaboratorial> ExamesLaboratoriais { get; set; } = new List<ExameLaboratorial>();
    public ICollection<RelatorioClinico> RelatoriosClinicos { get; set; } = new List<RelatorioClinico>();
    public ICollection<PlanoAlimentar> PlanosAlimentares { get; set; } = new List<PlanoAlimentar>();
    public ICollection<MetaPaciente> Metas { get; set; } = new List<MetaPaciente>();
    public ICollection<RegistroDiarioPaciente> RegistrosDiario { get; set; } = new List<RegistroDiarioPaciente>();
    public ICollection<PlanoTreino> PlanosTreino { get; set; } = new List<PlanoTreino>();
    public ICollection<ExecucaoTreino> ExecucoesTreino { get; set; } = new List<ExecucaoTreino>();
    public ICollection<PendenciaClinica> PendenciasClinicas { get; set; } = new List<PendenciaClinica>();
    public ICollection<InteracaoAcompanhamento> InteracoesAcompanhamento { get; set; } = new List<InteracaoAcompanhamento>();
    public ICollection<EvolucaoClinica> EvolucoesClinicas { get; set; } = new List<EvolucaoClinica>();
    public ICollection<FaseNutricional> FasesNutricionais { get; set; } = new List<FaseNutricional>();
    public ICollection<FaseTreino> FasesTreino { get; set; } = new List<FaseTreino>();
    public ICollection<CheckInAcompanhamento> CheckInsAcompanhamento { get; set; } = new List<CheckInAcompanhamento>();
    public ICollection<RevisaoFase> RevisoesFases { get; set; } = new List<RevisaoFase>();
}
