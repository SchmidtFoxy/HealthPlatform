using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/configuracoes")]
public class ConfiguracoesController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
    UserManager<Usuario> userManager) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo(CancellationToken ct)
    {
        var organizacao = await db.Organizacoes.AsNoTracking()
            .Where(x => x.Id == currentUser.OrganizationId)
            .Select(x => new { x.Id, x.Nome, x.Cnpj, x.Ativa })
            .FirstOrDefaultAsync(ct);

        var usuario = await db.Users.AsNoTracking()
            .Where(x => x.Id == currentUser.UserId)
            .Select(x => new { x.Id, x.Nome, x.Email, x.TipoUsuario, x.Ativo })
            .FirstOrDefaultAsync(ct);

        var profissional = await db.Profissionais.AsNoTracking()
            .Where(x => x.UsuarioId == currentUser.UserId &&
                        x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.RegistroProfissional,
                x.Especialidade,
                x.Ativo
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new { organizacao, usuario, profissional });
    }

    public sealed record AtualizarOrganizacaoRequest(string Nome, string? Cnpj);

    [HttpPut("organizacao")]
    public async Task<IActionResult> AtualizarOrganizacao(
        AtualizarOrganizacaoRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome da organizacao e obrigatorio." });

        var organizacao = await db.Organizacoes
            .FirstOrDefaultAsync(x => x.Id == currentUser.OrganizationId, ct);

        if (organizacao is null)
            return NotFound(new { message = "Organizacao nao encontrada." });

        var antes = new { organizacao.Nome, organizacao.Cnpj };

        organizacao.Nome = request.Nome.Trim();
        organizacao.Cnpj = string.IsNullOrWhiteSpace(request.Cnpj)
            ? null
            : request.Cnpj.Trim();
        organizacao.UpdatedAtUtc = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "UPDATE",
            Entidade = nameof(Organizacao),
            EntidadeId = organizacao.Id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(antes),
            DadosNovosJson = JsonSerializer.Serialize(new { organizacao.Nome, organizacao.Cnpj }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            organizacao.Id,
            organizacao.Nome,
            organizacao.Cnpj,
            organizacao.Ativa
        });
    }

    public sealed record AtualizarMinhaContaRequest(string Nome);
    public sealed record AlterarMinhaSenhaRequest(
        string SenhaAtual,
        string NovaSenha,
        string ConfirmacaoNovaSenha);

    [HttpGet("minha-conta")]
    public async Task<IActionResult> MinhaConta(CancellationToken ct)
    {
        var usuario = await db.Users.AsNoTracking()
            .Where(x =>
                x.Id == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Email,
                TipoUsuario = x.TipoUsuario.ToString(),
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        return usuario is null
            ? NotFound(new { message = "Usuario nao encontrado." })
            : Ok(usuario);
    }

    [HttpPut("minha-conta")]
    public async Task<IActionResult> AtualizarMinhaConta(
        AtualizarMinhaContaRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome e obrigatorio." });

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (usuario is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        var antes = new { usuario.Nome };

        usuario.Nome = request.Nome.Trim();

        var profissional = await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == usuario.Id &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (profissional is not null)
        {
            profissional.Nome = usuario.Nome;
            profissional.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "UPDATE",
            Entidade = "MinhaConta",
            EntidadeId = usuario.Id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(antes),
            DadosNovosJson = JsonSerializer.Serialize(new { usuario.Nome }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            TipoUsuario = usuario.TipoUsuario.ToString()
        });
    }

    [HttpPost("minha-conta/alterar-senha")]
    public async Task<IActionResult> AlterarMinhaSenha(
        AlterarMinhaSenhaRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SenhaAtual))
            return BadRequest(new { message = "Informe a senha atual." });

        if (string.IsNullOrWhiteSpace(request.NovaSenha) ||
            request.NovaSenha.Length < 10)
            return BadRequest(new { message = "A nova senha deve possuir pelo menos 10 caracteres e atender a politica de senha." });

        if (!string.Equals(request.NovaSenha, request.ConfirmacaoNovaSenha, StringComparison.Ordinal))
            return BadRequest(new { message = "A confirmacao da nova senha nao confere." });

        if (string.Equals(request.SenhaAtual, request.NovaSenha, StringComparison.Ordinal))
            return BadRequest(new { message = "A nova senha deve ser diferente da senha atual." });

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (usuario is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        var result = await userManager.ChangePasswordAsync(
            usuario,
            request.SenhaAtual,
            request.NovaSenha);

        if (!result.Succeeded)
            return BadRequest(new
            {
                message = string.Join("; ", result.Errors.Select(x => x.Description))
            });

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "PASSWORD_CHANGE",
            Entidade = "MinhaConta",
            EntidadeId = usuario.Id.ToString(),
            DadosAnterioresJson = null,
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                SenhaAlterada = true,
                AlteradaEmUtc = DateTime.UtcNow
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Senha alterada com sucesso." });
    }

}
