using System.Globalization;
using System.Security.Claims;
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
    public sealed record AtualizarPreferenciasRequest(
        string TemaPreferido,
        bool NotificarMensagens,
        bool NotificarAtualizacoesPlano,
        bool NotificarLembretes,
        bool NotificarCheckIns);
    public sealed record AtualizarFotoPerfilRequest(string? FotoDataUrl);
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
                x.FotoPerfilDataUrl,
                x.TemaPreferido,
                x.NotificarMensagens,
                x.NotificarAtualizacoesPlano,
                x.NotificarLembretes,
                x.NotificarCheckIns,
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

    [HttpPut("minha-conta/preferencias")]
    public async Task<IActionResult> AtualizarPreferencias(
        AtualizarPreferenciasRequest request,
        CancellationToken ct)
    {
        var tema = (request.TemaPreferido ?? string.Empty).Trim().ToLowerInvariant();
        if (tema is not ("light" or "dark"))
            return BadRequest(new { message = "Tema invalido. Use light ou dark." });

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (usuario is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        var antes = new
        {
            usuario.TemaPreferido,
            usuario.NotificarMensagens,
            usuario.NotificarAtualizacoesPlano,
            usuario.NotificarLembretes,
            usuario.NotificarCheckIns
        };

        usuario.TemaPreferido = tema;
        usuario.NotificarMensagens = request.NotificarMensagens;
        usuario.NotificarAtualizacoesPlano = request.NotificarAtualizacoesPlano;
        usuario.NotificarLembretes = request.NotificarLembretes;
        usuario.NotificarCheckIns = request.NotificarCheckIns;

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "UPDATE_PREFERENCES",
            Entidade = "MinhaConta",
            EntidadeId = usuario.Id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(antes),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                usuario.TemaPreferido,
                usuario.NotificarMensagens,
                usuario.NotificarAtualizacoesPlano,
                usuario.NotificarLembretes,
                usuario.NotificarCheckIns
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            usuario.TemaPreferido,
            usuario.NotificarMensagens,
            usuario.NotificarAtualizacoesPlano,
            usuario.NotificarLembretes,
            usuario.NotificarCheckIns
        });
    }

    [HttpPut("minha-conta/foto")]
    public async Task<IActionResult> AtualizarFotoPerfil(
        AtualizarFotoPerfilRequest request,
        CancellationToken ct)
    {
        var foto = string.IsNullOrWhiteSpace(request.FotoDataUrl) ? null : request.FotoDataUrl.Trim();
        if (foto is not null)
        {
            if (foto.Length > 400_000)
                return BadRequest(new { message = "A foto de perfil excede o limite permitido." });

            var tipoValido = foto.StartsWith("data:image/jpeg;base64,", StringComparison.OrdinalIgnoreCase) ||
                             foto.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase) ||
                             foto.StartsWith("data:image/webp;base64,", StringComparison.OrdinalIgnoreCase);
            if (!tipoValido)
                return BadRequest(new { message = "Formato de foto invalido. Use JPEG, PNG ou WebP." });
        }

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);
        if (usuario is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        usuario.FotoPerfilDataUrl = foto;
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = foto is null ? "PROFILE_PHOTO_REMOVE" : "PROFILE_PHOTO_UPDATE",
            Entidade = "MinhaConta",
            EntidadeId = usuario.Id.ToString(),
            DadosAnterioresJson = null,
            DadosNovosJson = JsonSerializer.Serialize(new { FotoAtualizada = foto is not null }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Ok(new { usuario.FotoPerfilDataUrl });
    }

    [HttpGet("minha-conta/sessao-atual")]
    public IActionResult SessaoAtual()
    {
        DateTime? expiraEmUtc = null;
        var exp = User.FindFirstValue("exp");
        if (long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expUnix))
            expiraEmUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

        return Ok(new
        {
            Ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
            ExpiraEmUtc = expiraEmUtc,
            RevogacaoLogout = "global",
            Observacao = "O logout atual revoga todas as sessoes emitidas anteriormente para esta conta."
        });
    }

}
