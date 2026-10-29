using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HealthPlatform.Api.Services;

/// <summary>
/// v0.72.1 - token do convite de primeiro acesso do paciente. O link vai por WhatsApp e o paciente
/// costuma abrir horas depois, entao ele vale mais que o token de "esqueci a senha" (30 minutos).
/// Continua valendo uma vez so: ao criar a senha o security stamp muda e o link morre.
/// </summary>
public sealed class ConvitePacienteTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<ConvitePacienteTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<Usuario>> logger)
    : DataProtectorTokenProvider<Usuario>(dataProtectionProvider, options, logger)
{
    public const string Nome = "ConvitePaciente";
    public static readonly TimeSpan Validade = TimeSpan.FromDays(7);
}

public sealed class ConvitePacienteTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public ConvitePacienteTokenProviderOptions()
    {
        Name = ConvitePacienteTokenProvider.Nome;
        TokenLifespan = ConvitePacienteTokenProvider.Validade;
    }
}
