using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class NotificacaoInterna : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid UsuarioId { get; set; }

    public string Tipo { get; set; } = "Sistema";
    public string Prioridade { get; set; } = "Normal";
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;

    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }
    public string OrigemChave { get; set; } = string.Empty;

    public DateTime? DataEventoUtc { get; set; }
    public string? Link { get; set; }

    public DateTime? LidaEmUtc { get; set; }
    public bool Ativa { get; set; } = true;

    public Organizacao Organizacao { get; set; } = null!;
}
