using System.Security.Claims;
using HealthPlatform.Api.Contracts.Auth;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager, IJwtTokenService jwt, AppDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await userManager.FindByEmailAsync(request.Email.Trim());
        if (usuario is null || !usuario.Ativo)
            return Unauthorized(new { message = "Email ou senha invalidos." });

        // Contas antigas podem ter sido criadas antes do hardening de lockout.
        if (!usuario.LockoutEnabled)
        {
            var enableLockout = await userManager.SetLockoutEnabledAsync(usuario, true);
            if (!enableLockout.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Nao foi possivel validar a politica de acesso." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(usuario, request.Senha, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Unauthorized(new { message = "Acesso temporariamente bloqueado apos tentativas invalidas. Tente novamente mais tarde." });
        if (!result.Succeeded)
            return Unauthorized(new { message = "Email ou senha invalidos." });

        await RegistrarAcessoAsync(usuario, "LOGIN");
        return Ok(await CriarRespostaAsync(usuario));
    }

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpPost("renovar")]
    public async Task<ActionResult<LoginResponse>> Renovar()
    {
        var usuario = await UsuarioAtualAsync();
        if (usuario is null || !usuario.Ativo)
            return Unauthorized(new { message = "Sessao expirada ou revogada." });

        if (!User.HasClaim("impersonation", "true"))
            await RegistrarAcessoAsync(usuario, "ACCESS");
        return Ok(await CriarRespostaAsync(usuario));
    }

    [Authorize(Policy = "AuthenticatedOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var usuario = await UsuarioAtualAsync();
        if (usuario is null)
            return NoContent();

        // JWT e stateless; rotacionar o SecurityStamp revoga imediatamente os tokens
        // emitidos antes deste logout. Por enquanto o logout e global entre dispositivos.
        var result = await userManager.UpdateSecurityStampAsync(usuario);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Nao foi possivel encerrar a sessao com seguranca." });

        return NoContent();
    }

    /// <summary>
    /// Guarda o ultimo acesso para o painel do administrador. A renovacao do token acontece
    /// a cada hora de uso, entao basta um registro a cada poucas horas.
    /// </summary>
    private async Task RegistrarAcessoAsync(Usuario usuario, string acao)
    {
        if (acao == "ACCESS")
        {
            var limite = DateTime.UtcNow.AddHours(-3);
            var recente = await db.AuditLogs.AsNoTracking().AnyAsync(x =>
                x.UsuarioId == usuario.Id &&
                (x.Acao == "LOGIN" || x.Acao == "ACCESS") &&
                x.CreatedAtUtc >= limite);
            if (recente) return;
        }

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = usuario.OrganizacaoId,
            UsuarioId = usuario.Id,
            Acao = acao,
            Entidade = "Sessao",
            EntidadeId = usuario.Id.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();
    }

    private async Task<LoginResponse> CriarRespostaAsync(Usuario usuario)
    {
        var roles = await userManager.GetRolesAsync(usuario);
        var token = jwt.Create(usuario, roles.ToArray());
        return new LoginResponse(token.Token, token.ExpiresAtUtc, usuario.Nome, usuario.TipoUsuario.ToString());
    }

    private async Task<Usuario?> UsuarioAtualAsync()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? await userManager.FindByIdAsync(userId.ToString())
            : null;
    }
}
