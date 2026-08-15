using System.Text.Json;
using HealthPlatform.Api.Contracts.Profissionais;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profissionais")]
public class ProfissionaisController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ProfissionalResponse>> GetMe(CancellationToken ct)
    {
        var profissional = await db.Profissionais.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId, ct);

        return profissional is null
            ? NotFound(new { message = "Perfil profissional ainda nao cadastrado." })
            : Ok(ToResponse(profissional));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfissionalResponse>> UpsertMe(UpsertMeuProfissionalRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome e obrigatorio." });
        if (string.IsNullOrWhiteSpace(request.RegistroProfissional))
            return BadRequest(new { message = "Registro profissional e obrigatorio." });

        var registro = request.RegistroProfissional.Trim().ToUpperInvariant();
        var conflito = await db.Profissionais.AnyAsync(x =>
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.RegistroProfissional == registro &&
            x.UsuarioId != currentUser.UserId, ct);

        if (conflito)
            return Conflict(new { message = "Este registro profissional ja esta vinculado a outro usuario da organizacao." });

        var profissional = await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (profissional is null)
        {
            profissional = new Profissional
            {
                OrganizacaoId = currentUser.OrganizationId,
                UsuarioId = currentUser.UserId,
                Nome = request.Nome.Trim(),
                RegistroProfissional = registro,
                Especialidade = Normalizar(request.Especialidade),
                Tipo = TipoUsuario.Medico,
                Ativo = true
            };
            db.Profissionais.Add(profissional);
            AdicionarAuditoria("CREATE", profissional, null, Snapshot(profissional));
        }
        else
        {
            var antes = Snapshot(profissional);
            profissional.Nome = request.Nome.Trim();
            profissional.RegistroProfissional = registro;
            profissional.Especialidade = Normalizar(request.Especialidade);
            profissional.Ativo = true;
            AdicionarAuditoria("UPDATE", profissional, antes, Snapshot(profissional));
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(profissional));
    }

    private void AdicionarAuditoria(string acao, Profissional profissional, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Profissional),
            EntidadeId = profissional.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static object Snapshot(Profissional x) => new
    {
        x.Id,
        x.UsuarioId,
        x.Nome,
        x.RegistroProfissional,
        x.Especialidade,
        x.Tipo,
        x.Ativo
    };

    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProfissionalResponse ToResponse(Profissional x) => new(
        x.Id, x.UsuarioId, x.Nome, x.RegistroProfissional, x.Especialidade,
        x.Tipo.ToString(), x.Ativo, x.CreatedAtUtc);
}
