using HealthPlatform.Domain.Common;
using HealthPlatform.Domain.Enums;

namespace HealthPlatform.Domain.Entities;

public class Profissional : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string RegistroProfissional { get; set; } = string.Empty;
    public string? Especialidade { get; set; }
    public TipoUsuario Tipo { get; set; } = TipoUsuario.Medico;
    public bool Ativo { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public ICollection<Anamnese> Anamneses { get; set; } = new List<Anamnese>();
    public ICollection<PerguntaAnamnese> PerguntasAnamnese { get; set; } = new List<PerguntaAnamnese>();
    public ICollection<ExameLaboratorial> ExamesLaboratoriais { get; set; } = new List<ExameLaboratorial>();
    public ICollection<RelatorioClinico> RelatoriosClinicos { get; set; } = new List<RelatorioClinico>();
    public ICollection<PlanoAlimentar> PlanosAlimentares { get; set; } = new List<PlanoAlimentar>();
    public ICollection<ModeloPlanoAlimentar> ModelosPlanosAlimentares { get; set; } = new List<ModeloPlanoAlimentar>();
    public ICollection<ModeloPlanoTreino> ModelosPlanosTreino { get; set; } = new List<ModeloPlanoTreino>();
    public ICollection<ModeloRefeicao> ModelosRefeicoes { get; set; } = new List<ModeloRefeicao>();
    public ICollection<ModeloSessaoTreino> ModelosSessoesTreino { get; set; } = new List<ModeloSessaoTreino>();
    public ICollection<MetaPaciente> Metas { get; set; } = new List<MetaPaciente>();
    public ICollection<PlanoTreino> PlanosTreino { get; set; } = new List<PlanoTreino>();
    public ICollection<PendenciaClinica> PendenciasClinicas { get; set; } = new List<PendenciaClinica>();
    public ICollection<InteracaoAcompanhamento> InteracoesAcompanhamento { get; set; } = new List<InteracaoAcompanhamento>();
    public ICollection<EvolucaoClinica> EvolucoesClinicas { get; set; } = new List<EvolucaoClinica>();
    public ICollection<FaseNutricional> FasesNutricionais { get; set; } = new List<FaseNutricional>();
    public ICollection<FaseTreino> FasesTreino { get; set; } = new List<FaseTreino>();
}
