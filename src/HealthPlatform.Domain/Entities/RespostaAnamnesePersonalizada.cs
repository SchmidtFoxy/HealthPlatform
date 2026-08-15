using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class RespostaAnamnesePersonalizada : BaseEntity
{
    public Guid AnamneseId { get; set; }
    public Guid PerguntaAnamneseId { get; set; }
    public string? Resposta { get; set; }

    public Anamnese Anamnese { get; set; } = null!;
    public PerguntaAnamnese PerguntaAnamnese { get; set; } = null!;
}
