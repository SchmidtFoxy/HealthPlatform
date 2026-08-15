using HealthPlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HealthPlatform.Infrastructure.Identity;

public class Usuario : IdentityUser<Guid>
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoUsuario TipoUsuario { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
