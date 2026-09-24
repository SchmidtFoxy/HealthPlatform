using System.Security.Claims;
using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record ImpersonacaoResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Nome,
    string TipoUsuario,
    bool Impersonating,
    Guid? ImpersonatorId,
    string? ImpersonatorNome);

[ApiController]
[Route("api/impersonacao")]
public sealed class ImpersonacaoController(
    AppDbContext db,
    CurrentUser currentUser,
    UserManager<Usuario> userManager,
    IJwtTokenService jwt,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly TipoUsuario[] TiposSimulaveis =
    [
        TipoUsuario.Medico,
        TipoUsuario.Nutricionista,
        TipoUsuario.Personal
    ];

    [Authorize(Roles = "Admin")]
    [HttpPost("{usuarioId:guid}/iniciar")]
    public async Task<ActionResult<ImpersonacaoResponse>> Iniciar(Guid usuarioId, CancellationToken ct = default)
    {
        if (User.HasClaim("impersonation", "true"))
            return BadRequest(new { message = "Finalize a simulacao atual antes de iniciar outra." });

        var admin = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);
        if (admin is null) return Forbid();

        var alvo = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == usuarioId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (alvo is null)
            return NotFound(new { message = "Profissional nao encontrado ou inativo." });
        if (!TiposSimulaveis.Contains(alvo.TipoUsuario))
            return BadRequest(new { message = "A simulacao e permitida apenas para medico, nutricionista ou personal ativos." });

        var roles = await userManager.GetRolesAsync(alvo);
        var token = jwt.Create(alvo, roles.ToArray(), new Dictionary<string, string>
        {
            ["impersonation"] = "true",
            ["impersonator_id"] = admin.Id.ToString(),
            ["impersonator_name"] = admin.Nome,
            ["impersonator_security_stamp"] = admin.SecurityStamp ?? string.Empty
        });

        AdicionarAuditoria(admin.Id, "IMPERSONATION_START", alvo.Id, null, new
        {
            AlvoUsuarioId = alvo.Id,
            alvo.Nome,
            TipoUsuario = alvo.TipoUsuario.ToString(),
            Modo = "READ_ONLY"
        });
        await db.SaveChangesAsync(ct);

        return Ok(new ImpersonacaoResponse(
            token.Token, token.ExpiresAtUtc, alvo.Nome, alvo.TipoUsuario.ToString(),
            true, admin.Id, admin.Nome));
    }

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpPost("finalizar")]
    public async Task<ActionResult<ImpersonacaoResponse>> Finalizar(CancellationToken ct = default)
    {
        if (!User.HasClaim("impersonation", "true"))
            return BadRequest(new { message = "Nao existe simulacao ativa nesta sessao." });

        var adminValue = User.FindFirstValue("impersonator_id");
        if (!Guid.TryParse(adminValue, out var adminId))
            return Unauthorized(new { message = "Sessao de simulacao invalida." });

        var admin = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == adminId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);
        if (admin is null)
            return Unauthorized(new { message = "Administrador de origem nao esta mais autorizado." });

        var alvoId = currentUser.UserId;
        var alvoNome = User.FindFirstValue(ClaimTypes.Name) ?? "Profissional";
        var roles = await userManager.GetRolesAsync(admin);
        var token = jwt.Create(admin, roles.ToArray());

        AdicionarAuditoria(admin.Id, "IMPERSONATION_END", alvoId, new
        {
            AlvoUsuarioId = alvoId,
            AlvoNome = alvoNome,
            Modo = "READ_ONLY"
        }, null);
        await db.SaveChangesAsync(ct);

        return Ok(new ImpersonacaoResponse(
            token.Token, token.ExpiresAtUtc, admin.Nome, admin.TipoUsuario.ToString(),
            false, null, null));
    }

    private void AdicionarAuditoria(Guid atorId, string acao, Guid alvoId, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = atorId,
            Acao = acao,
            Entidade = "ImpersonacaoProfissional",
            EntidadeId = alvoId.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
}
