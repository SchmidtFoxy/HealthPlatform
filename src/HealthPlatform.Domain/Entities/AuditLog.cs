using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? OrganizacaoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string? EntidadeId { get; set; }
    public string? DadosAnterioresJson { get; set; }
    public string? DadosNovosJson { get; set; }
    public string? IpAddress { get; set; }
}
