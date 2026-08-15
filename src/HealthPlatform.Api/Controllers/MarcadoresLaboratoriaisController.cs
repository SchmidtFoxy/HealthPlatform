using System.Globalization;
using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.MarcadoresLaboratoriais;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/exames/marcadores")]
public class MarcadoresLaboratoriaisController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MarcadorLaboratorialResponse>>> GetAll(
        [FromQuery] string? busca = null,
        [FromQuery] bool incluirInativos = false,
        CancellationToken ct = default)
    {
        var query = db.MarcadoresLaboratoriais.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && (incluirInativos || x.Ativo));

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Nome, $"%{termo}%") ||
                                     (x.Categoria != null && EF.Functions.ILike(x.Categoria, $"%{termo}%")));
        }

        var itens = await query.OrderBy(x => x.Categoria).ThenBy(x => x.Nome).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MarcadorLaboratorialResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.MarcadoresLaboratoriais.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Marcador laboratorial nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<MarcadorLaboratorialResponse>> Create(UpsertMarcadorLaboratorialRequest request, CancellationToken ct)
    {
        var nome = request.Nome.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return BadRequest(new { message = "Nome do marcador e obrigatorio." });
        var normalizado = Normalizar(nome);
        if (await db.MarcadoresLaboratoriais.AnyAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == normalizado, ct))
            return Conflict(new { message = "Ja existe um marcador com este nome na organizacao." });

        var item = new MarcadorLaboratorial
        {
            OrganizacaoId = currentUser.OrganizationId,
            Nome = nome,
            NomeNormalizado = normalizado,
            Categoria = Limpar(request.Categoria),
            UnidadePadrao = Limpar(request.UnidadePadrao)
        };
        db.MarcadoresLaboratoriais.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MarcadorLaboratorialResponse>> Update(Guid id, UpsertMarcadorLaboratorialRequest request, CancellationToken ct)
    {
        var item = await db.MarcadoresLaboratoriais.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Marcador laboratorial nao encontrado." });
        var nome = request.Nome.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return BadRequest(new { message = "Nome do marcador e obrigatorio." });
        var normalizado = Normalizar(nome);
        if (await db.MarcadoresLaboratoriais.AnyAsync(x => x.Id != id && x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == normalizado, ct))
            return Conflict(new { message = "Ja existe outro marcador com este nome na organizacao." });

        var antes = Snapshot(item);
        item.Nome = nome;
        item.NomeNormalizado = normalizado;
        item.Categoria = Limpar(request.Categoria);
        item.UnidadePadrao = Limpar(request.UnidadePadrao);
        Auditar("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var item = await db.MarcadoresLaboratoriais.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Marcador laboratorial nao encontrado." });
        if (!item.Ativo) return NoContent();
        var antes = Snapshot(item);
        item.Ativo = false;
        Auditar("DEACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativar")]
    public async Task<ActionResult<MarcadorLaboratorialResponse>> Reactivate(Guid id, CancellationToken ct)
    {
        var item = await db.MarcadoresLaboratoriais.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Marcador laboratorial nao encontrado." });
        var antes = Snapshot(item);
        item.Ativo = true;
        Auditar("ACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    private void Auditar(string acao, MarcadorLaboratorial item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId,
        UsuarioId = currentUser.UserId,
        Acao = acao,
        Entidade = nameof(MarcadorLaboratorial),
        EntidadeId = item.Id.ToString(),
        DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
        DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
        IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    });

    private static object Snapshot(MarcadorLaboratorial x) => new { x.Id, x.Nome, x.Categoria, x.UnidadePadrao, x.Ativo };
    private static MarcadorLaboratorialResponse ToResponse(MarcadorLaboratorial x) => new(x.Id, x.Nome, x.Categoria, x.UnidadePadrao, x.Ativo, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string Normalizar(string valor)
    {
        var decomposed = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
