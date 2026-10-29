using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Auth;
using HealthPlatform.Api.Services.Email;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth/password")]
public sealed class PasswordRecoveryController(
    AppDbContext db,
    UserManager<Usuario> userManager,
    ITransactionalEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private const string GenericRecoveryMessage =
        "Se existir uma conta ativa para este e-mail, você receberá as instruções para redefinir sua senha.";

    private readonly EmailOptions _emailOptions = emailOptions.Value;

    [HttpPost("esqueci")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (email is null)
            return Ok(new { message = GenericRecoveryMessage });

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.Ativo || string.IsNullOrWhiteSpace(user.Email))
            return Ok(new { message = GenericRecoveryMessage });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetUrl = BuildResetUrl(user.Email, encodedToken);
        var template = AccountEmailTemplates.PasswordReset(user.Nome, resetUrl);
        var delivery = await emailService.SendAsync(user.Email, template.Subject, template.Html, template.Text, ct);

        await AuditAsync(user,
            delivery.Sent ? "PASSWORD_RESET_SENT" : "PASSWORD_RESET_DELIVERY_PENDING",
            new { email = MaskEmail(user.Email), provider = delivery.Provider, delivery.Sent, delivery.Error }, ct);

        // Nunca revela se a conta existe nem se o provedor SMTP está habilitado.
        return Ok(new { message = GenericRecoveryMessage });
    }

    [HttpPost("redefinir")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        if (request.NovaSenha != request.ConfirmacaoNovaSenha)
            return BadRequest(new { message = "A confirmação da nova senha não confere." });

        var email = NormalizeEmail(request.Email);
        if (email is null || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Solicitação de redefinição inválida ou expirada." });

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.Ativo)
            return BadRequest(new { message = "Solicitação de redefinição inválida ou expirada." });

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return BadRequest(new { message = "Solicitação de redefinição inválida ou expirada." });
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NovaSenha);
        if (!result.Succeeded)
        {
            var passwordErrors = result.Errors
                .Where(x => x.Code.Contains("Password", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Description)
                .ToArray();

            if (passwordErrors.Length > 0)
                return BadRequest(new { message = "A nova senha não atende à política de segurança.", errors = passwordErrors });

            return BadRequest(new { message = "Solicitação de redefinição inválida ou expirada." });
        }

        await AuditAsync(user, "PASSWORD_RESET_COMPLETED", new { email = MaskEmail(user.Email) }, ct);
        return Ok(new { message = "Senha redefinida com sucesso. Você já pode entrar com a nova senha." });
    }

    private string BuildResetUrl(string email, string token)
    {
        var baseUrl = _emailOptions.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/redefinir-senha?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private async Task AuditAsync(Usuario user, string action, object data, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = user.OrganizacaoId,
            UsuarioId = user.Id,
            Acao = action,
            Entidade = nameof(Usuario),
            EntidadeId = user.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(data),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(ct);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var value = email.Trim().ToLowerInvariant();
        try { _ = new System.Net.Mail.MailAddress(value); return value; }
        catch { return null; }
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at < 0) return "***";
        if (at <= 1) return "***" + email[at..];
        return email[..1] + "***" + email[(at - 1)..];
    }
}
