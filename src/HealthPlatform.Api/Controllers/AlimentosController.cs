using System.Globalization;
using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Alimentos;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alimentos")]
public class AlimentosController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AlimentoResponse>>> GetAll([FromQuery] string? busca = null, [FromQuery] bool incluirInativos = false, CancellationToken ct = default)
    {
        var query = db.Alimentos.AsNoTracking().Where(x => x.OrganizacaoId == currentUser.OrganizationId && (incluirInativos || x.Ativo));
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Nome, $"%{termo}%") || (x.Categoria != null && EF.Functions.ILike(x.Categoria, $"%{termo}%")));
        }
        var itens = await query.OrderBy(x => x.Categoria).ThenBy(x => x.Nome).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlimentoResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Alimentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Alimento nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<AlimentoResponse>> Create(UpsertAlimentoRequest request, CancellationToken ct)
    {
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var nome = request.Nome.Trim(); var normalizado = Normalizar(nome);
        if (await db.Alimentos.AnyAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == normalizado, ct))
            return Conflict(new { message = "Ja existe um alimento com este nome na organizacao." });
        var item = new Alimento { OrganizacaoId = currentUser.OrganizationId, Nome = nome, NomeNormalizado = normalizado, Categoria = Limpar(request.Categoria), CaloriasPor100g = request.CaloriasPor100g, ProteinasPor100g = request.ProteinasPor100g, CarboidratosPor100g = request.CarboidratosPor100g, GordurasPor100g = request.GordurasPor100g, FibrasPor100g = request.FibrasPor100g };
        db.Alimentos.Add(item); Auditar("CREATE", item, null, Snapshot(item)); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlimentoResponse>> Update(Guid id, UpsertAlimentoRequest request, CancellationToken ct)
    {
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var item = await db.Alimentos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Alimento nao encontrado." });
        var nome = request.Nome.Trim(); var normalizado = Normalizar(nome);
        if (await db.Alimentos.AnyAsync(x => x.Id != id && x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == normalizado, ct))
            return Conflict(new { message = "Ja existe outro alimento com este nome na organizacao." });
        var antes = Snapshot(item);
        item.Nome = nome; item.NomeNormalizado = normalizado; item.Categoria = Limpar(request.Categoria); item.CaloriasPor100g = request.CaloriasPor100g; item.ProteinasPor100g = request.ProteinasPor100g; item.CarboidratosPor100g = request.CarboidratosPor100g; item.GordurasPor100g = request.GordurasPor100g; item.FibrasPor100g = request.FibrasPor100g; item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("UPDATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var item = await db.Alimentos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Alimento nao encontrado." });
        if (!item.Ativo) return NoContent(); var antes = Snapshot(item); item.Ativo = false; item.UpdatedAtUtc = DateTime.UtcNow; Auditar("DEACTIVATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("{id:guid}/reativar")]
    public async Task<ActionResult<AlimentoResponse>> Reactivate(Guid id, CancellationToken ct)
    {
        var item = await db.Alimentos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Alimento nao encontrado." }); var antes = Snapshot(item); item.Ativo = true; item.UpdatedAtUtc = DateTime.UtcNow; Auditar("ACTIVATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    private static string? Validar(UpsertAlimentoRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return "Nome do alimento e obrigatorio.";
        if (r.CaloriasPor100g < 0 || r.ProteinasPor100g < 0 || r.CarboidratosPor100g < 0 || r.GordurasPor100g < 0 || r.FibrasPor100g < 0) return "Valores nutricionais nao podem ser negativos.";
        return null;
    }
    private void Auditar(string acao, Alimento item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao, Entidade = nameof(Alimento), EntidadeId = item.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private static object Snapshot(Alimento x) => new { x.Id, x.Nome, x.Categoria, x.CaloriasPor100g, x.ProteinasPor100g, x.CarboidratosPor100g, x.GordurasPor100g, x.FibrasPor100g, x.Ativo };
    private static AlimentoResponse ToResponse(Alimento x) => new(x.Id, x.Nome, x.Categoria, x.CaloriasPor100g, x.ProteinasPor100g, x.CarboidratosPor100g, x.GordurasPor100g, x.FibrasPor100g, x.Ativo, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static string Normalizar(string valor) { var d = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD); var sb = new StringBuilder(); foreach (var c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c); return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries)); }
}
