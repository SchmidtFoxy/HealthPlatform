using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record AdminAvisoPushRequest(string Publico, string Titulo, string Mensagem);

public sealed record AdminAvisoPushPreviaResponse(string Publico, int Pessoas, int Aparelhos);

public sealed record AdminAvisoPushResultadoResponse(int Pessoas, int Aparelhos, int Enviadas, int Falhas, bool Habilitado);

/// <summary>Aviso push do admin para varias pessoas da organizacao (ex.: notificacao de teste).</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/push")]
public sealed class AdminPushController(
    AppDbContext db,
    CurrentUser currentUser,
    IPushNotificationService push,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly string[] Publicos = ["todos", "equipe", "admins"];

    /// <summary>Quantas pessoas e aparelhos recebem, sem enviar nada.</summary>
    [HttpGet("previa")]
    public async Task<ActionResult<AdminAvisoPushPreviaResponse>> Previa([FromQuery] string publico = "todos", CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();
        if (!Publicos.Contains(publico)) return BadRequest(new { message = "Publico invalido." });
        var alvos = await AlvosAsync(publico, ct);
        return Ok(new AdminAvisoPushPreviaResponse(publico, alvos.Count, alvos.Sum(x => x.Aparelhos)));
    }

    [HttpPost("enviar")]
    public async Task<ActionResult<AdminAvisoPushResultadoResponse>> Enviar(AdminAvisoPushRequest request, CancellationToken ct)
    {
        if (!await EhAdmin(ct)) return Forbid();
        if (!Publicos.Contains(request.Publico)) return BadRequest(new { message = "Publico invalido." });
        var titulo = request.Titulo?.Trim() ?? "";
        var mensagem = request.Mensagem?.Trim() ?? "";
        if (titulo.Length is 0 or > 80 || mensagem.Length is 0 or > 240)
            return BadRequest(new { message = "Titulo (ate 80) e mensagem (ate 240 caracteres) sao obrigatorios." });

        var alvos = await AlvosAsync(request.Publico, ct);
        int enviadas = 0, falhas = 0, aparelhos = 0;
        var habilitado = true;
        foreach (var alvo in alvos)
        {
            var r = await push.EnviarAsync(alvo.UsuarioId, "aviso", titulo, mensagem, "inicio", ct);
            enviadas += r.Enviadas;
            falhas += r.Falhas;
            aparelhos += r.Registradas;
            habilitado &= r.Habilitado;
        }

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "PUSH_BROADCAST",
            Entidade = "PushSubscription",
            EntidadeId = currentUser.OrganizationId.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new { request.Publico, Titulo = titulo, Mensagem = mensagem, Pessoas = alvos.Count, Enviadas = enviadas, Falhas = falhas }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(ct);

        return Ok(new AdminAvisoPushResultadoResponse(alvos.Count, aparelhos, enviadas, falhas, habilitado && alvos.Count > 0));
    }

    private sealed record Alvo(Guid UsuarioId, int Aparelhos);

    /// <summary>Pessoas ativas da organizacao com ao menos um aparelho inscrito no push.</summary>
    private async Task<List<Alvo>> AlvosAsync(string publico, CancellationToken ct)
    {
        var org = currentUser.OrganizationId;
        var usuarios = db.Users.AsNoTracking().Where(x => x.OrganizacaoId == org && x.Ativo);
        usuarios = publico switch
        {
            "equipe" => usuarios.Where(x => x.TipoUsuario != TipoUsuario.Paciente),
            "admins" => usuarios.Where(x => x.TipoUsuario == TipoUsuario.Admin),
            _ => usuarios
        };
        var ids = usuarios.Select(x => x.Id);
        return await db.Set<IdentityUserToken<Guid>>().AsNoTracking()
            .Where(x => x.LoginProvider == WebPushNotificationService.LoginProvider && ids.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new Alvo(g.Key, g.Count()))
            .ToListAsync(ct);
    }

    private async Task<bool> EhAdmin(CancellationToken ct) =>
        !User.HasClaim("impersonation", "true") &&
        await db.Users.AsNoTracking().AnyAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);
}
