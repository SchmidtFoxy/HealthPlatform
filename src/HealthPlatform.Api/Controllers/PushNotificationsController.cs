using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "AuthenticatedOnly")]
[Route("api/push")]
public sealed class PushNotificationsController(
    AppDbContext db,
    CurrentUser currentUser,
    IOptions<PushOptions> options,
    IPushNotificationService push) : ControllerBase
{
    private readonly PushOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record RegistrarPushRequest(string Endpoint, string P256Dh, string Auth, string? UserAgent);
    public sealed record RemoverPushRequest(string Endpoint);

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var total = await db.Set<IdentityUserToken<Guid>>().AsNoTracking().CountAsync(x =>
            x.UserId == currentUser.UserId && x.LoginProvider == WebPushNotificationService.LoginProvider, ct);
        return Ok(new
        {
            habilitadoNoServidor = _options.Enabled,
            configurado = _options.Enabled && !string.IsNullOrWhiteSpace(_options.PublicKey),
            publicKey = _options.Enabled ? _options.PublicKey : null,
            dispositivos = total,
            ambienteSeguro = Request.IsHttps || HttpContext.Request.Host.Host is "localhost" or "127.0.0.1"
        });
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> Registrar(RegistrarPushRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
            return BadRequest(new { message = "Endpoint de push invalido." });
        if (string.IsNullOrWhiteSpace(request.P256Dh) || string.IsNullOrWhiteSpace(request.Auth))
            return BadRequest(new { message = "Chaves da inscricao push sao obrigatorias." });

        var name = NomeToken(request.Endpoint);
        var tokens = db.Set<IdentityUserToken<Guid>>();
        var token = await tokens.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId && x.LoginProvider == WebPushNotificationService.LoginProvider && x.Name == name, ct);
        var payload = JsonSerializer.Serialize(new PushSubscriptionData(request.Endpoint.Trim(), request.P256Dh.Trim(), request.Auth.Trim(), Limitar(request.UserAgent, 500), DateTime.UtcNow), JsonOptions);
        if (token is null)
        {
            tokens.Add(new IdentityUserToken<Guid> { UserId = currentUser.UserId, LoginProvider = WebPushNotificationService.LoginProvider, Name = name, Value = payload });
        }
        else token.Value = payload;

        Auditar("PUSH_SUBSCRIBE", new { Dispositivo = name, UserAgent = Limitar(request.UserAgent, 180) });
        await db.SaveChangesAsync(ct);
        return Ok(new { inscrito = true, dispositivo = name });
    }

    [HttpDelete("subscriptions")]
    public async Task<IActionResult> Remover(RemoverPushRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint)) return BadRequest(new { message = "Endpoint obrigatorio." });
        var name = NomeToken(request.Endpoint);
        var token = await db.Set<IdentityUserToken<Guid>>().FirstOrDefaultAsync(x => x.UserId == currentUser.UserId && x.LoginProvider == WebPushNotificationService.LoginProvider && x.Name == name, ct);
        if (token is not null) db.Remove(token);
        Auditar("PUSH_UNSUBSCRIBE", new { Dispositivo = name });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<IActionResult> Testar(CancellationToken ct)
    {
        var result = await push.EnviarAsync(currentUser.UserId, "lembrete", "AESYN Performance", "Notificacoes push configuradas para este dispositivo.", "inicio", ct);
        return Ok(result);
    }

    private void Auditar(string acao, object dados) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId,
        UsuarioId = currentUser.UserId,
        Acao = acao,
        Entidade = "PushSubscription",
        EntidadeId = currentUser.UserId.ToString(),
        DadosNovosJson = JsonSerializer.Serialize(dados, JsonOptions),
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
    });

    private static string NomeToken(string endpoint)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(endpoint.Trim()));
        return Convert.ToHexString(hash)[..32];
    }

    private static string? Limitar(string? valor, int limite) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim()[..Math.Min(valor.Trim().Length, limite)];
}
