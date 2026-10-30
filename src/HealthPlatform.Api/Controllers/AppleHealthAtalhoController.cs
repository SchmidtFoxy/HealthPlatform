using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

/// <summary>
/// v0.73.0 — piloto Apple Health / Apple Watch pelo app Atalhos do iPhone.
/// O paciente gera um token no perfil; o atalho envia o resumo do dia com esse token.
/// </summary>
[ApiController]
public sealed class AppleHealthAtalhoController(AppDbContext db, CurrentUser currentUser, IConfiguration configuration) : ControllerBase
{
    private const string CabecalhoToken = "X-Aesyn-Token";

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/apple-health")]
    public async Task<IActionResult> Status(CancellationToken ct = default)
    {
        var (ativo, criado, ultimoUso) = await AppleHealthAtalhoService.StatusAsync(db, currentUser.UserId, ct);
        return Ok(new
        {
            ativo,
            criadoEmUtc = criado,
            ultimoEnvioUtc = ultimoUso,
            linkAtalho = LinkAtalho(),
            urlEnvio = UrlEnvio()
        });
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/portal/me/apple-health/token")]
    public async Task<IActionResult> GerarToken(CancellationToken ct = default)
    {
        if (!await PacienteAtivoAsync(currentUser.UserId, ct))
            return NotFound(new { message = "Paciente vinculado nao encontrado." });
        var token = await AppleHealthAtalhoService.GerarTokenAsync(db, currentUser.UserId, ct);
        return Ok(new { token, linkAtalho = LinkAtalho(), urlEnvio = UrlEnvio() });
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpDelete("api/portal/me/apple-health/token")]
    public async Task<IActionResult> Desconectar(CancellationToken ct = default)
    {
        await AppleHealthAtalhoService.RevogarAsync(db, currentUser.UserId, ct);
        return NoContent();
    }

    /// <summary>Chamado pelo atalho do iPhone. Sem login: autentica pelo token pessoal.</summary>
    [AllowAnonymous]
    [HttpPost("api/integracoes/apple-health/atalho")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Receber([FromBody] JsonElement corpo, CancellationToken ct = default)
    {
        var token = Request.Headers[CabecalhoToken].ToString();
        if (string.IsNullOrWhiteSpace(token) && corpo.ValueKind == JsonValueKind.Object &&
            corpo.TryGetProperty("token", out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String)
            token = tokenEl.GetString() ?? string.Empty;

        var usuarioId = await AppleHealthAtalhoService.ValidarAsync(db, token, ct);
        if (usuarioId is null)
            return Unauthorized(new { ok = false, mensagem = "Token do Aesyn invalido ou desconectado. Gere um novo em Perfil > Apple Watch e Saude." });

        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        var usuarioAtivo = await db.Users.AsNoTracking().AnyAsync(x => x.Id == usuarioId && x.Ativo && x.ContaDesativadaEmUtc == null, ct);
        if (pacienteId is null || !usuarioAtivo)
            return Unauthorized(new { ok = false, mensagem = "Conta de paciente inativa no Aesyn." });

        var resultado = await AppleHealthAtalhoService.GravarResumoDiarioAsync(db, pacienteId.Value, corpo, ct);
        if (!resultado.Ok) return BadRequest(new { ok = false, mensagem = resultado.Mensagem });
        await db.SaveChangesAsync(ct);
        return Ok(new { ok = true, mensagem = resultado.Mensagem, dia = resultado.Dia?.ToString("yyyy-MM-dd"), importados = resultado.Importados });
    }

    private Task<bool> PacienteAtivoAsync(Guid usuarioId, CancellationToken ct) =>
        db.Pacientes.AsNoTracking().AnyAsync(x => x.UsuarioId == usuarioId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private string? LinkAtalho()
    {
        var link = configuration["AppleHealth:AtalhoUrl"];
        return string.IsNullOrWhiteSpace(link) ? null : link.Trim();
    }

    private string UrlEnvio() => $"{Request.Scheme}://{Request.Host}/api/integracoes/apple-health/atalho";
}
