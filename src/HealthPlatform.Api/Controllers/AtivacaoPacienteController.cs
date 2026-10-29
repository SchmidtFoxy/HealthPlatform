using System.Text.Json;
using HealthPlatform.Api.Contracts.AcessoPaciente;
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

[ApiController]
[Route("api/auth/paciente")]
public sealed class AtivacaoPacienteController(
    AppDbContext db,
    UserManager<Usuario> userManager,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private const string ConviteExpirado =
        "Este link de convite expirou ou já foi usado. Peça um novo convite ao seu profissional.";

    [AllowAnonymous]
    [HttpPost("ativar")]
    public async Task<IActionResult> Ativar(AtivarPacienteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { message = "E-mail, token e senha sao obrigatorios." });

        var usuario = await userManager.FindByEmailAsync(request.Email.Trim());
        if (usuario is null || usuario.TipoUsuario != TipoUsuario.Paciente)
            return BadRequest(new { message = ConviteExpirado });

        var paciente = await db.Pacientes.FirstOrDefaultAsync(x =>
            x.UsuarioId == usuario.Id &&
            x.OrganizacaoId == usuario.OrganizacaoId &&
            x.Ativo, ct);

        if (paciente is null)
            return BadRequest(new { message = "Paciente vinculado nao encontrado ou inativo." });

        // Convites a partir da v0.72.1 valem 7 dias (ConvitePacienteTokenProvider). Links gerados antes
        // ainda usam o token de redefinicao de senha, que segue aceito enquanto nao expira.
        var token = request.Token.Trim();
        var conviteValido = await userManager.VerifyUserTokenAsync(
            usuario,
            ConvitePacienteTokenProvider.Nome,
            UserManager<Usuario>.ResetPasswordTokenPurpose,
            token);
        if (conviteValido)
            token = await userManager.GeneratePasswordResetTokenAsync(usuario);

        var reset = await userManager.ResetPasswordAsync(usuario, token, request.Senha);
        if (!reset.Succeeded)
        {
            if (reset.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return BadRequest(new { message = ConviteExpirado });

            return BadRequest(new { message = string.Join("; ", reset.Errors.Select(x => x.Description)) });
        }

        usuario.Ativo = true;
        usuario.EmailConfirmed = true;

        var update = await userManager.UpdateAsync(usuario);
        if (!update.Succeeded)
            return BadRequest(new { message = string.Join("; ", update.Errors.Select(x => x.Description)) });

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = usuario.OrganizacaoId,
            UsuarioId = usuario.Id,
            Acao = "ACTIVATE_ACCESS",
            Entidade = nameof(Paciente),
            EntidadeId = paciente.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new { usuario.Email, Ativo = true }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Acesso ativado com sucesso. Agora voce ja pode entrar." });
    }
}
