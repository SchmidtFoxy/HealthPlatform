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
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/acesso")]
public sealed class AcessoPacienteController(
    AppDbContext db,
    CurrentUser currentUser,
    UserManager<Usuario> userManager,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AcessoPacienteStatusResponse>> Status(Guid pacienteId, CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado." });

        if (!paciente.UsuarioId.HasValue)
            return Ok(new AcessoPacienteStatusResponse(paciente.Id, false, false, paciente.Email));

        var usuario = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == paciente.UsuarioId.Value &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        return Ok(new AcessoPacienteStatusResponse(
            paciente.Id,
            usuario is not null,
            usuario?.Ativo == true,
            usuario?.Email ?? paciente.Email));
    }

    [HttpPost]
    public async Task<ActionResult<ConvitePacienteResponse>> CriarOuRenovar(
        Guid pacienteId,
        CriarAcessoPacienteRequest request,
        CancellationToken ct)
    {
        var paciente = await db.Pacientes
            .FirstOrDefaultAsync(x =>
                x.Id == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado ou inativo." });

        var email = string.IsNullOrWhiteSpace(request.Email)
            ? paciente.Email?.Trim()
            : request.Email.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Informe um e-mail para liberar o acesso do paciente." });

        Usuario? usuario = null;
        var novoUsuario = false;

        if (paciente.UsuarioId.HasValue)
            usuario = await userManager.FindByIdAsync(paciente.UsuarioId.Value.ToString());

        if (usuario is null)
        {
            var existentePorEmail = await userManager.FindByEmailAsync(email);
            if (existentePorEmail is not null)
                return BadRequest(new { message = "Este e-mail ja pertence a outro usuario da plataforma." });

            usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                OrganizacaoId = currentUser.OrganizationId,
                Nome = paciente.Nome,
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                TipoUsuario = TipoUsuario.Paciente,
                Ativo = false
            };

            var create = await userManager.CreateAsync(usuario);
            if (!create.Succeeded)
                return BadRequest(new { message = string.Join("; ", create.Errors.Select(x => x.Description)) });

            var role = await userManager.AddToRoleAsync(usuario, TipoUsuario.Paciente.ToString());
            if (!role.Succeeded)
            {
                await userManager.DeleteAsync(usuario);
                return BadRequest(new { message = string.Join("; ", role.Errors.Select(x => x.Description)) });
            }

            paciente.UsuarioId = usuario.Id;
            paciente.Email = email;
            novoUsuario = true;
        }
        else
        {
            if (usuario.OrganizacaoId != currentUser.OrganizationId || usuario.TipoUsuario != TipoUsuario.Paciente)
                return BadRequest(new { message = "O usuario vinculado nao e um acesso de paciente valido." });

            if (!string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var outro = await userManager.FindByEmailAsync(email);
                if (outro is not null && outro.Id != usuario.Id)
                    return BadRequest(new { message = "Este e-mail ja pertence a outro usuario da plataforma." });

                usuario.Email = email;
                usuario.UserName = email;
                var updateEmail = await userManager.UpdateAsync(usuario);
                if (!updateEmail.Succeeded)
                    return BadRequest(new { message = string.Join("; ", updateEmail.Errors.Select(x => x.Description)) });

                paciente.Email = email;
            }

            // Renovar convite desativa o login ate uma nova ativacao.
            usuario.Ativo = false;
            usuario.EmailConfirmed = false;
            await userManager.UpdateAsync(usuario);
        }

        paciente.UpdatedAtUtc = DateTime.UtcNow;

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = novoUsuario ? "CREATE_ACCESS" : "RENEW_ACCESS",
            Entidade = nameof(Paciente),
            EntidadeId = paciente.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                paciente.UsuarioId,
                Email = email,
                TipoUsuario = TipoUsuario.Paciente.ToString()
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);

        return Ok(new ConvitePacienteResponse(paciente.Id, email, token, novoUsuario));
    }

    [HttpDelete]
    public async Task<IActionResult> Revogar(Guid pacienteId, CancellationToken ct)
    {
        var paciente = await db.Pacientes
            .FirstOrDefaultAsync(x =>
                x.Id == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado." });

        if (!paciente.UsuarioId.HasValue)
            return NoContent();

        var usuario = await userManager.FindByIdAsync(paciente.UsuarioId.Value.ToString());
        if (usuario is not null)
        {
            usuario.Ativo = false;
            await userManager.UpdateAsync(usuario);
        }

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "REVOKE_ACCESS",
            Entidade = nameof(Paciente),
            EntidadeId = paciente.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new { paciente.UsuarioId, Ativo = false }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
