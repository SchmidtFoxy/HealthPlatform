using HealthPlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HealthPlatform.Infrastructure.Identity;

public class Usuario : IdentityUser<Guid>
{
    public Guid OrganizacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoUsuario TipoUsuario { get; set; }
    public bool Ativo { get; set; } = true;
    public string? FotoPerfilDataUrl { get; set; }
    public string TemaPreferido { get; set; } = "light";
    public bool NotificarMensagens { get; set; } = true;
    public bool NotificarAtualizacoesPlano { get; set; } = true;
    public bool NotificarLembretes { get; set; } = true;
    public bool NotificarCheckIns { get; set; } = true;
    public DateTime? OnboardingPacienteConcluidoEmUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
