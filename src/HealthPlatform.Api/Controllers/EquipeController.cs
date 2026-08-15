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

public sealed record EquipeMembroResponse(
    Guid UsuarioId,
    string Nome,
    string Email,
    string TipoUsuario,
    bool Ativo,
    DateTime CreatedAtUtc,
    Guid? ProfissionalId,
    string? RegistroProfissional,
    string? Especialidade,
    bool? PerfilProfissionalAtivo,
    bool EhUsuarioAtual);

public sealed record CriarEquipeMembroRequest(
    string Nome,
    string Email,
    string TipoUsuario,
    string SenhaTemporaria,
    string? RegistroProfissional,
    string? Especialidade);

public sealed record AtualizarEquipeMembroRequest(
    string Nome,
    string TipoUsuario,
    bool Ativo,
    string? RegistroProfissional,
    string? Especialidade);

public sealed record RedefinirSenhaEquipeRequest(
    string NovaSenhaTemporaria);

[ApiController]
[Authorize]
[Route("api/equipe")]
public sealed class EquipeController(
    AppDbContext db,
    CurrentUser currentUser,
    UserManager<Usuario> userManager,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly TipoUsuario[] TiposEquipe =
    [
        TipoUsuario.Admin,
        TipoUsuario.Medico,
        TipoUsuario.Nutricionista,
        TipoUsuario.Personal,
        TipoUsuario.Secretaria
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EquipeMembroResponse>>> Listar(
        [FromQuery] bool incluirInativos = true,
        [FromQuery] string? busca = null,
        [FromQuery] string? tipo = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var query = db.Users.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.TipoUsuario != TipoUsuario.Paciente &&
                (incluirInativos || x.Ativo));

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termo) ||
                (x.Email != null && EF.Functions.ILike(x.Email, termo)));
        }

        if (!string.IsNullOrWhiteSpace(tipo) && TryTipoEquipe(tipo, out var tipoFiltro))
            query = query.Where(x => x.TipoUsuario == tipoFiltro);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("ativo", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Ativo);
            else if (status.Equals("inativo", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => !x.Ativo);
        }

        var usuarios = await query
            .OrderByDescending(x => x.Ativo)
            .ThenBy(x => x.Nome)
            .ToListAsync(ct);

        var ids = usuarios.Select(x => x.Id).ToArray();
        var profissionais = await db.Profissionais.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                ids.Contains(x.UsuarioId))
            .ToDictionaryAsync(x => x.UsuarioId, ct);

        return Ok(usuarios.Select(x =>
        {
            profissionais.TryGetValue(x.Id, out var p);
            return ToResponse(x, p);
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<EquipeMembroResponse>> Criar(
        CriarEquipeMembroRequest request,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var validation = ValidarBase(request.Nome, request.Email, request.TipoUsuario);
        if (validation is not null) return validation;

        if (string.IsNullOrWhiteSpace(request.SenhaTemporaria) || request.SenhaTemporaria.Length < 10)
            return BadRequest(new { message = "A senha temporaria deve possuir pelo menos 10 caracteres e atender a politica de senha." });

        var tipo = Enum.Parse<TipoUsuario>(request.TipoUsuario, ignoreCase: true);
        var registro = NormalizarRegistro(request.RegistroProfissional);
        if (ExigePerfilProfissional(tipo) && string.IsNullOrWhiteSpace(registro))
            return BadRequest(new { message = "Registro profissional e obrigatorio para medico, nutricionista e personal." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpperInvariant(), ct))
            return Conflict(new { message = "Ja existe um usuario com este e-mail." });

        if (!string.IsNullOrWhiteSpace(registro) &&
            await db.Profissionais.AnyAsync(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.RegistroProfissional == registro, ct))
            return Conflict(new { message = "Este registro profissional ja esta em uso na organizacao." });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            OrganizacaoId = currentUser.OrganizationId,
            Nome = request.Nome.Trim(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TipoUsuario = tipo,
            Ativo = true
        };

        var created = await userManager.CreateAsync(usuario, request.SenhaTemporaria);
        if (!created.Succeeded)
            return BadRequest(new { message = string.Join("; ", created.Errors.Select(x => x.Description)) });

        var roleResult = await userManager.AddToRoleAsync(usuario, tipo.ToString());
        if (!roleResult.Succeeded)
            return BadRequest(new { message = string.Join("; ", roleResult.Errors.Select(x => x.Description)) });

        Profissional? profissional = null;
        if (ExigePerfilProfissional(tipo))
        {
            profissional = new Profissional
            {
                OrganizacaoId = currentUser.OrganizationId,
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                RegistroProfissional = registro!,
                Especialidade = Normalizar(request.Especialidade),
                Tipo = tipo,
                Ativo = true
            };
            db.Profissionais.Add(profissional);
        }

        AdicionarAuditoria(
            "CREATE",
            "UsuarioEquipe",
            usuario.Id,
            null,
            new
            {
                usuario.Nome,
                usuario.Email,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                usuario.Ativo,
                RegistroProfissional = profissional?.RegistroProfissional,
                profissional?.Especialidade
            });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return CreatedAtAction(nameof(Listar), null, ToResponse(usuario, profissional));
    }

    [HttpPut("{usuarioId:guid}")]
    public async Task<ActionResult<EquipeMembroResponse>> Atualizar(
        Guid usuarioId,
        AtualizarEquipeMembroRequest request,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == usuarioId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.TipoUsuario != TipoUsuario.Paciente, ct);

        if (usuario is null)
            return NotFound(new { message = "Membro da equipe nao encontrado." });

        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome e obrigatorio." });

        if (!TryTipoEquipe(request.TipoUsuario, out var tipo))
            return BadRequest(new { message = "Tipo de usuario invalido para a equipe." });

        if (usuario.Id == currentUser.UserId && (!request.Ativo || tipo != TipoUsuario.Admin))
            return BadRequest(new { message = "O administrador atual nao pode remover o proprio acesso administrativo." });

        var registro = NormalizarRegistro(request.RegistroProfissional);
        if (ExigePerfilProfissional(tipo) && string.IsNullOrWhiteSpace(registro))
            return BadRequest(new { message = "Registro profissional e obrigatorio para medico, nutricionista e personal." });

        if (!string.IsNullOrWhiteSpace(registro) &&
            await db.Profissionais.AnyAsync(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.RegistroProfissional == registro &&
                x.UsuarioId != usuarioId, ct))
            return Conflict(new { message = "Este registro profissional ja esta em uso na organizacao." });

        var profissional = await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == usuarioId &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        var antes = new
        {
            usuario.Nome,
            usuario.Email,
            TipoUsuario = usuario.TipoUsuario.ToString(),
            usuario.Ativo,
            RegistroProfissional = profissional?.RegistroProfissional,
            profissional?.Especialidade,
            PerfilProfissionalAtivo = profissional?.Ativo
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var roles = await userManager.GetRolesAsync(usuario);
        if (roles.Count > 0)
        {
            var removeRoles = await userManager.RemoveFromRolesAsync(usuario, roles);
            if (!removeRoles.Succeeded)
                return BadRequest(new { message = string.Join("; ", removeRoles.Errors.Select(x => x.Description)) });
        }

        var addRole = await userManager.AddToRoleAsync(usuario, tipo.ToString());
        if (!addRole.Succeeded)
            return BadRequest(new { message = string.Join("; ", addRole.Errors.Select(x => x.Description)) });

        usuario.Nome = request.Nome.Trim();
        usuario.TipoUsuario = tipo;
        usuario.Ativo = request.Ativo;

        var updated = await userManager.UpdateAsync(usuario);
        if (!updated.Succeeded)
            return BadRequest(new { message = string.Join("; ", updated.Errors.Select(x => x.Description)) });

        if (ExigePerfilProfissional(tipo))
        {
            if (profissional is null)
            {
                profissional = new Profissional
                {
                    OrganizacaoId = currentUser.OrganizationId,
                    UsuarioId = usuario.Id,
                    Nome = usuario.Nome,
                    RegistroProfissional = registro!,
                    Especialidade = Normalizar(request.Especialidade),
                    Tipo = tipo,
                    Ativo = request.Ativo
                };
                db.Profissionais.Add(profissional);
            }
            else
            {
                profissional.Nome = usuario.Nome;
                profissional.RegistroProfissional = registro!;
                profissional.Especialidade = Normalizar(request.Especialidade);
                profissional.Tipo = tipo;
                profissional.Ativo = request.Ativo;
                profissional.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        else if (profissional is not null)
        {
            profissional.Nome = usuario.Nome;
            profissional.Tipo = tipo;
            profissional.Ativo = false;
            profissional.UpdatedAtUtc = DateTime.UtcNow;
        }

        AdicionarAuditoria(
            "UPDATE",
            "UsuarioEquipe",
            usuario.Id,
            antes,
            new
            {
                usuario.Nome,
                usuario.Email,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                usuario.Ativo,
                RegistroProfissional = profissional?.RegistroProfissional,
                profissional?.Especialidade,
                PerfilProfissionalAtivo = profissional?.Ativo
            });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(ToResponse(usuario, profissional));
    }

    [HttpPost("{usuarioId:guid}/redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(
        Guid usuarioId,
        RedefinirSenhaEquipeRequest request,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        if (usuarioId == currentUser.UserId)
            return BadRequest(new { message = "Use a troca de senha da propria conta para alterar sua senha." });

        if (string.IsNullOrWhiteSpace(request.NovaSenhaTemporaria) ||
            request.NovaSenhaTemporaria.Length < 10)
            return BadRequest(new { message = "A nova senha temporaria deve possuir pelo menos 10 caracteres e atender a politica de senha." });

        var usuario = await db.Users.FirstOrDefaultAsync(x =>
            x.Id == usuarioId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.TipoUsuario != TipoUsuario.Paciente, ct);

        if (usuario is null)
            return NotFound(new { message = "Membro da equipe nao encontrado." });

        if (!usuario.Ativo)
            return BadRequest(new { message = "Reative o acesso antes de redefinir a senha." });

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var result = await userManager.ResetPasswordAsync(
            usuario,
            token,
            request.NovaSenhaTemporaria);

        if (!result.Succeeded)
            return BadRequest(new
            {
                message = string.Join("; ", result.Errors.Select(x => x.Description))
            });

        AdicionarAuditoria(
            "PASSWORD_RESET",
            "UsuarioEquipe",
            usuario.Id,
            new
            {
                usuario.Nome,
                usuario.Email,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                usuario.Ativo
            },
            new
            {
                usuario.Nome,
                usuario.Email,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                usuario.Ativo,
                SenhaTemporariaRedefinida = true
            });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            message = "Senha temporaria redefinida com sucesso."
        });
    }

    private async Task<bool> EhAdmin(CancellationToken ct) =>
        await db.Users.AsNoTracking().AnyAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);

    private ActionResult? ValidarBase(string nome, string email, string tipoUsuario)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return BadRequest(new { message = "Nome e obrigatorio." });
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "E-mail invalido." });
        if (!TryTipoEquipe(tipoUsuario, out _))
            return BadRequest(new { message = "Tipo de usuario invalido para a equipe." });
        return null;
    }

    private static bool TryTipoEquipe(string value, out TipoUsuario tipo)
    {
        if (!Enum.TryParse(value, ignoreCase: true, out tipo))
            return false;
        return TiposEquipe.Contains(tipo);
    }

    private static bool ExigePerfilProfissional(TipoUsuario tipo) =>
        tipo is TipoUsuario.Medico or TipoUsuario.Nutricionista or TipoUsuario.Personal;

    private static string? Normalizar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizarRegistro(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private void AdicionarAuditoria(string acao, string entidade, Guid entidadeId, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private EquipeMembroResponse ToResponse(Usuario usuario, Profissional? profissional) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email ?? string.Empty,
        usuario.TipoUsuario.ToString(),
        usuario.Ativo,
        usuario.CreatedAtUtc,
        profissional?.Id,
        profissional?.RegistroProfissional,
        profissional?.Especialidade,
        profissional?.Ativo,
        usuario.Id == currentUser.UserId);
}
