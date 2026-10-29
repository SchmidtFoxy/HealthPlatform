using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Conta;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Email;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Route("api/conta/email")]
public sealed class ContaEmailController(
    AppDbContext db,
    CurrentUser currentUser,
    UserManager<Usuario> userManager,
    ITransactionalEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpGet]
    public async Task<ActionResult<EmailAccountStatusResponse>> Status(CancellationToken ct)
    {
        var user = await FindCurrentUserAsync(ct);
        if (user is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        return Ok(new EmailAccountStatusResponse(
            user.Email,
            user.EmailConfirmed,
            !user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Email)));
    }

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpPost("confirmacao/solicitar")]
    public async Task<IActionResult> RequestConfirmation(CancellationToken ct)
    {
        var user = await FindCurrentUserAsync(ct);
        if (user is null)
            return NotFound(new { message = "Usuario nao encontrado." });
        if (string.IsNullOrWhiteSpace(user.Email))
            return BadRequest(new { message = "A conta nao possui email principal cadastrado." });
        if (user.EmailConfirmed)
            return Ok(new { message = "Email ja confirmado.", sent = false });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var url = BuildPublicUrl("confirmar-email", user.Id, encodedToken, null);
        var template = AccountEmailTemplates.Confirmation(user.Nome, url);
        var delivery = await emailService.SendAsync(user.Email, template.Subject, template.Html, template.Text, ct);

        await AuditAsync(user, delivery.Sent ? "EMAIL_CONFIRMATION_SENT" : "EMAIL_CONFIRMATION_PENDING",
            new { provider = delivery.Provider, delivery.Sent, delivery.Error }, ct);

        return Ok(new
        {
            message = delivery.Sent
                ? "Email de confirmacao enviado."
                : "Solicitacao registrada. O provedor de email nao esta habilitado neste ambiente.",
            sent = delivery.Sent
        });
    }

    [AllowAnonymous]
    [HttpPost("confirmacao/confirmar")]
    public async Task<IActionResult> Confirm(ConfirmEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UsuarioId.ToString());
        if (user is null || !user.Ativo)
            return BadRequest(new { message = "Solicitacao de confirmacao invalida." });

        string token;
        try { token = DecodeToken(request.Token); }
        catch { return BadRequest(new { message = "Token de confirmacao invalido." }); }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return BadRequest(new { message = "Token de confirmacao invalido ou expirado." });

        await AuditAsync(user, "EMAIL_CONFIRMED", new { user.Email }, ct);
        return Ok(new { message = "Email confirmado com sucesso." });
    }

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpPost("troca/solicitar")]
    public async Task<IActionResult> RequestChange(RequestEmailChangeRequest request, CancellationToken ct)
    {
        var user = await FindCurrentUserAsync(ct);
        if (user is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        var newEmail = NormalizeEmail(request.NovoEmail);
        if (newEmail is null)
            return BadRequest(new { message = "Informe um email valido." });
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "O novo email deve ser diferente do email atual." });
        if (!await userManager.CheckPasswordAsync(user, request.SenhaAtual))
            return BadRequest(new { message = "Senha atual invalida." });

        var duplicate = await userManager.FindByEmailAsync(newEmail);
        if (duplicate is not null && duplicate.Id != user.Id)
            return Conflict(new { message = "Este email ja esta cadastrado em outra conta." });

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var encodedToken = EncodeToken(token);
        var url = BuildPublicUrl("confirmar-troca-email", user.Id, encodedToken, newEmail);
        var template = AccountEmailTemplates.ChangeEmail(user.Nome, newEmail, url);
        var delivery = await emailService.SendAsync(newEmail, template.Subject, template.Html, template.Text, ct);

        await AuditAsync(user, delivery.Sent ? "EMAIL_CHANGE_SENT" : "EMAIL_CHANGE_DELIVERY_PENDING",
            new { novoEmail = MaskEmail(newEmail), provider = delivery.Provider, delivery.Sent, delivery.Error }, ct);

        return Ok(new
        {
            message = delivery.Sent
                ? "Confirmacao enviada para o novo email."
                : "Solicitacao registrada. O provedor de email nao esta habilitado neste ambiente.",
            sent = delivery.Sent
        });
    }

    [AllowAnonymous]
    [HttpPost("troca/confirmar")]
    public async Task<IActionResult> ConfirmChange(ConfirmEmailChangeRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UsuarioId.ToString());
        if (user is null || !user.Ativo)
            return BadRequest(new { message = "Solicitacao de troca invalida." });

        var newEmail = NormalizeEmail(request.NovoEmail);
        if (newEmail is null)
            return BadRequest(new { message = "Email invalido." });

        var duplicate = await userManager.FindByEmailAsync(newEmail);
        if (duplicate is not null && duplicate.Id != user.Id)
            return Conflict(new { message = "Este email ja esta cadastrado em outra conta." });

        string token;
        try { token = DecodeToken(request.Token); }
        catch { return BadRequest(new { message = "Token de troca invalido." }); }

        var oldEmail = user.Email;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
            return BadRequest(new { message = "Token de troca invalido ou expirado." });

        var userNameResult = await userManager.SetUserNameAsync(user, newEmail);
        if (!userNameResult.Succeeded)
            return BadRequest(new { message = "Nao foi possivel concluir a troca do identificador de login." });

        var paciente = await db.Pacientes.FirstOrDefaultAsync(
            x => x.UsuarioId == user.Id && x.OrganizacaoId == user.OrganizacaoId, ct);
        if (paciente is not null)
            paciente.Email = newEmail;

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = user.OrganizacaoId,
            UsuarioId = user.Id,
            Acao = "EMAIL_CHANGED",
            Entidade = nameof(Usuario),
            EntidadeId = user.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                emailAnterior = MaskEmail(oldEmail),
                emailAtual = MaskEmail(newEmail)
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(new { message = "Email principal alterado e confirmado com sucesso." });
    }

    private Task<Usuario?> FindCurrentUserAsync(CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

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

    private string BuildPublicUrl(string path, Guid userId, string token, string? email)
    {
        var baseUrl = _emailOptions.PublicBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{path}?userId={Uri.EscapeDataString(userId.ToString())}&token={Uri.EscapeDataString(token)}";
        if (!string.IsNullOrWhiteSpace(email))
            url += $"&email={Uri.EscapeDataString(email)}";
        return url;
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    private static string DecodeToken(string token) => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var value = email.Trim().ToLowerInvariant();
        try { _ = new System.Net.Mail.MailAddress(value); return value; }
        catch { return null; }
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.IndexOf('@');
        if (at < 0) return "***";
        if (at <= 1) return "***" + email[at..];
        return email[..1] + "***" + email[(at - 1)..];
    }
}
