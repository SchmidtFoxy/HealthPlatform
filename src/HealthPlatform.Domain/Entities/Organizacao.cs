using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class Organizacao : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public bool Ativa { get; set; } = true;

    public ICollection<Profissional> Profissionais { get; set; } = new List<Profissional>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    public ICollection<PerguntaAnamnese> PerguntasAnamnese { get; set; } = new List<PerguntaAnamnese>();
    public ICollection<MarcadorLaboratorial> MarcadoresLaboratoriais { get; set; } = new List<MarcadorLaboratorial>();
    public ICollection<Alimento> Alimentos { get; set; } = new List<Alimento>();
    public ICollection<Exercicio> Exercicios { get; set; } = new List<Exercicio>();
    public ICollection<PendenciaClinica> PendenciasClinicas { get; set; } = new List<PendenciaClinica>();
    public ICollection<NotificacaoInterna> NotificacoesInternas { get; set; } = new List<NotificacaoInterna>();
    public ICollection<InteracaoAcompanhamento> InteracoesAcompanhamento { get; set; } = new List<InteracaoAcompanhamento>();
    public ICollection<EvolucaoClinica> EvolucoesClinicas { get; set; } = new List<EvolucaoClinica>();
    public ICollection<ModeloPlanoAlimentar> ModelosPlanosAlimentares { get; set; } = new List<ModeloPlanoAlimentar>();
    public ICollection<ModeloPlanoTreino> ModelosPlanosTreino { get; set; } = new List<ModeloPlanoTreino>();
    public ICollection<ModeloRefeicao> ModelosRefeicoes { get; set; } = new List<ModeloRefeicao>();
    public ICollection<ModeloSessaoTreino> ModelosSessoesTreino { get; set; } = new List<ModeloSessaoTreino>();
    public ICollection<FaseNutricional> FasesNutricionais { get; set; } = new List<FaseNutricional>();
    public ICollection<FaseTreino> FasesTreino { get; set; } = new List<FaseTreino>();
}
