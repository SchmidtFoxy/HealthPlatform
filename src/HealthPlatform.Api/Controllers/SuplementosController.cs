using System.Globalization;
using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Suplementos;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/suplementos")]
public class SuplementosController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly HashSet<string> Categorias = new(StringComparer.OrdinalIgnoreCase)
    {
        "Proteína", "Creatina", "Pré-treino", "Barrinha proteica", "Eletrólitos", "Vitaminas e minerais", "Carboidrato", "Recuperação", "Outros"
    };

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SuplementoResponse>>> GetAll(
        [FromQuery] string? busca = null,
        [FromQuery] string? categoria = null,
        [FromQuery] bool incluirInativos = false,
        CancellationToken ct = default)
    {
        var query = db.Suplementos.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && (incluirInativos || x.Ativo));

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, $"%{termo}%") ||
                (x.Marca != null && EF.Functions.ILike(x.Marca, $"%{termo}%")) ||
                EF.Functions.ILike(x.Categoria, $"%{termo}%") ||
                (x.Composicao != null && EF.Functions.ILike(x.Composicao, $"%{termo}%")));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var filtro = categoria.Trim();
            query = query.Where(x => x.Categoria == filtro);
        }

        var itens = await query.OrderBy(x => x.Categoria).ThenBy(x => x.Marca).ThenBy(x => x.Nome).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SuplementoResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Suplementos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Suplemento nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<SuplementoResponse>> Create(UpsertSuplementoRequest request, CancellationToken ct)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(new { message = erro });

        var nome = request.Nome.Trim();
        var marca = Limpar(request.Marca);
        var nomeNormalizado = Normalizar(nome);
        var marcaNormalizada = Normalizar(marca ?? string.Empty);
        if (await db.Suplementos.AnyAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == nomeNormalizado && x.MarcaNormalizada == marcaNormalizada, ct))
            return Conflict(new { message = "Ja existe um suplemento com este nome e fabricante na organizacao." });

        var item = new Suplemento
        {
            OrganizacaoId = currentUser.OrganizationId,
            Nome = nome,
            NomeNormalizado = nomeNormalizado,
            Marca = marca,
            MarcaNormalizada = marcaNormalizada,
            Categoria = NormalizarCategoria(request.Categoria),
            Forma = Limpar(request.Forma),
            PorcaoQuantidade = request.PorcaoQuantidade,
            PorcaoUnidade = request.PorcaoUnidade.Trim(),
            CaloriasPorPorcao = request.CaloriasPorPorcao,
            ProteinasGPorPorcao = request.ProteinasGPorPorcao,
            CarboidratosGPorPorcao = request.CarboidratosGPorPorcao,
            GordurasGPorPorcao = request.GordurasGPorPorcao,
            FibrasGPorPorcao = request.FibrasGPorPorcao,
            CafeinaMgPorPorcao = request.CafeinaMgPorPorcao,
            Composicao = Limpar(request.Composicao),
            InstrucoesUso = Limpar(request.InstrucoesUso),
            Observacoes = Limpar(request.Observacoes)
        };
        db.Suplementos.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SuplementoResponse>> Update(Guid id, UpsertSuplementoRequest request, CancellationToken ct)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(new { message = erro });

        var item = await db.Suplementos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Suplemento nao encontrado." });

        var nome = request.Nome.Trim();
        var marca = Limpar(request.Marca);
        var nomeNormalizado = Normalizar(nome);
        var marcaNormalizada = Normalizar(marca ?? string.Empty);
        if (await db.Suplementos.AnyAsync(x => x.Id != id && x.OrganizacaoId == currentUser.OrganizationId && x.NomeNormalizado == nomeNormalizado && x.MarcaNormalizada == marcaNormalizada, ct))
            return Conflict(new { message = "Ja existe outro suplemento com este nome e fabricante na organizacao." });

        var antes = Snapshot(item);
        item.Nome = nome;
        item.NomeNormalizado = nomeNormalizado;
        item.Marca = marca;
        item.MarcaNormalizada = marcaNormalizada;
        item.Categoria = NormalizarCategoria(request.Categoria);
        item.Forma = Limpar(request.Forma);
        item.PorcaoQuantidade = request.PorcaoQuantidade;
        item.PorcaoUnidade = request.PorcaoUnidade.Trim();
        item.CaloriasPorPorcao = request.CaloriasPorPorcao;
        item.ProteinasGPorPorcao = request.ProteinasGPorPorcao;
        item.CarboidratosGPorPorcao = request.CarboidratosGPorPorcao;
        item.GordurasGPorPorcao = request.GordurasGPorPorcao;
        item.FibrasGPorPorcao = request.FibrasGPorPorcao;
        item.CafeinaMgPorPorcao = request.CafeinaMgPorPorcao;
        item.Composicao = Limpar(request.Composicao);
        item.InstrucoesUso = Limpar(request.InstrucoesUso);
        item.Observacoes = Limpar(request.Observacoes);
        item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var item = await db.Suplementos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Suplemento nao encontrado." });
        if (!item.Ativo) return NoContent();
        var antes = Snapshot(item);
        item.Ativo = false;
        item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("DEACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativar")]
    public async Task<ActionResult<SuplementoResponse>> Reactivate(Guid id, CancellationToken ct)
    {
        var item = await db.Suplementos.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Suplemento nao encontrado." });
        var antes = Snapshot(item);
        item.Ativo = true;
        item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("ACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    private static string? Validar(UpsertSuplementoRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return "Nome do suplemento e obrigatorio.";
        if (string.IsNullOrWhiteSpace(r.Categoria)) return "Categoria do suplemento e obrigatoria.";
        if (string.IsNullOrWhiteSpace(r.PorcaoUnidade)) return "Unidade da porcao e obrigatoria.";
        if (r.PorcaoQuantidade <= 0) return "Quantidade da porcao deve ser maior que zero.";
        if (r.CaloriasPorPorcao < 0 || r.ProteinasGPorPorcao < 0 || r.CarboidratosGPorPorcao < 0 || r.GordurasGPorPorcao < 0 || r.FibrasGPorPorcao < 0 || r.CafeinaMgPorPorcao < 0)
            return "Valores nutricionais e cafeina nao podem ser negativos.";
        return null;
    }

    private static string NormalizarCategoria(string valor)
    {
        var categoria = valor.Trim();
        return Categorias.FirstOrDefault(x => x.Equals(categoria, StringComparison.OrdinalIgnoreCase)) ?? categoria;
    }

    private void Auditar(string acao, Suplemento item, object? antes, object? depois) =>
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Suplemento),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static object Snapshot(Suplemento x) => new
    {
        x.Id, x.Nome, x.Marca, x.Categoria, x.Forma, x.PorcaoQuantidade, x.PorcaoUnidade,
        x.CaloriasPorPorcao, x.ProteinasGPorPorcao, x.CarboidratosGPorPorcao, x.GordurasGPorPorcao,
        x.FibrasGPorPorcao, x.CafeinaMgPorPorcao, x.Composicao, x.InstrucoesUso, x.Observacoes, x.Ativo
    };

    private static SuplementoResponse ToResponse(Suplemento x) => new(
        x.Id, x.Nome, x.Marca, x.Categoria, x.Forma, x.PorcaoQuantidade, x.PorcaoUnidade,
        x.CaloriasPorPorcao, x.ProteinasGPorPorcao, x.CarboidratosGPorPorcao, x.GordurasGPorPorcao,
        x.FibrasGPorPorcao, x.CafeinaMgPorPorcao, x.Composicao, x.InstrucoesUso, x.Observacoes,
        x.Ativo, x.CreatedAtUtc, x.UpdatedAtUtc);

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static string Normalizar(string valor)
    {
        var d = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
