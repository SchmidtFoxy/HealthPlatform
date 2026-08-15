using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class PerguntaAnamnese : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid ProfissionalId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string TipoResposta { get; set; } = "Texto";
    public string? OpcoesJson { get; set; }
    public int Ordem { get; set; }
    public bool Ativa { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
    public ICollection<RespostaAnamnesePersonalizada> Respostas { get; set; } = new List<RespostaAnamnesePersonalizada>();
}
