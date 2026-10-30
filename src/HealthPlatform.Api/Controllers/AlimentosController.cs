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
        var item = new Alimento { OrganizacaoId = currentUser.OrganizationId, Nome = nome, NomeNormalizado = normalizado, Categoria = Limpar(request.Categoria), CaloriasPor100g = request.CaloriasPor100g, ProteinasPor100g = request.ProteinasPor100g, CarboidratosPor100g = request.CarboidratosPor100g, GordurasPor100g = request.GordurasPor100g, FibrasPor100g = request.FibrasPor100g, UnidadeNome = Limpar(request.UnidadeNome), GramasPorUnidade = request.GramasPorUnidade, GramasPorMl = request.GramasPorMl };
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
        item.Nome = nome; item.NomeNormalizado = normalizado; item.Categoria = Limpar(request.Categoria); item.CaloriasPor100g = request.CaloriasPor100g; item.ProteinasPor100g = request.ProteinasPor100g; item.CarboidratosPor100g = request.CarboidratosPor100g; item.GordurasPor100g = request.GordurasPor100g; item.FibrasPor100g = request.FibrasPor100g; item.UnidadeNome = Limpar(request.UnidadeNome); item.GramasPorUnidade = request.GramasPorUnidade; item.GramasPorMl = request.GramasPorMl; item.UpdatedAtUtc = DateTime.UtcNow;
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

    /// <summary>Acrescenta os alimentos da base Aesyn que ainda nao existem na organizacao (compara pelo nome).</summary>
    [HttpPost("catalogo-base")]
    public async Task<IActionResult> PopularCatalogoBase(CancellationToken ct)
    {
        var existentes = (await db.Alimentos.Where(x => x.OrganizacaoId == currentUser.OrganizationId).Select(x => x.NomeNormalizado).ToListAsync(ct)).ToHashSet();
        var criados = new List<string>();
        foreach (var b in CatalogoBase)
        {
            var normalizado = Normalizar(b.Nome);
            if (!existentes.Add(normalizado)) continue;
            var item = new Alimento { OrganizacaoId = currentUser.OrganizationId, Nome = b.Nome, NomeNormalizado = normalizado, Categoria = b.Categoria, CaloriasPor100g = b.Kcal, ProteinasPor100g = b.Prot, CarboidratosPor100g = b.Carb, GordurasPor100g = b.Gord, FibrasPor100g = b.Fibra, UnidadeNome = b.Medida, GramasPorUnidade = b.GramasMedida, GramasPorMl = b.GramasPorMl };
            db.Alimentos.Add(item); Auditar("CREATE", item, null, Snapshot(item)); criados.Add(b.Nome);
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { criados = criados.Count, ignorados = CatalogoBase.Length - criados.Count, nomes = criados });
    }

    [HttpGet("favoritos")]
    public async Task<ActionResult<IReadOnlyCollection<AlimentoFavoritoResponse>>> GetFavoritos(CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct); if (profissional is null) return Ok(Array.Empty<AlimentoFavoritoResponse>());
        var itens = await db.AlimentosFavoritos.AsNoTracking().Where(x => x.ProfissionalId == profissional.Id).OrderBy(x => x.CreatedAtUtc).Select(x => new AlimentoFavoritoResponse(x.Refeicao, x.AlimentoId)).ToListAsync(ct);
        return Ok(itens);
    }

    /// <summary>Troca a lista de favoritos de uma refeicao (identificada pelo nome) do profissional logado.</summary>
    [HttpPut("favoritos")]
    public async Task<ActionResult<IReadOnlyCollection<AlimentoFavoritoResponse>>> SalvarFavoritos(SalvarFavoritosRefeicaoRequest request, CancellationToken ct)
    {
        var refeicao = Normalizar(request.Refeicao ?? string.Empty);
        if (refeicao.Length is 0 or > 80) return BadRequest(new { message = "Informe o nome da refeicao (ate 80 caracteres)." });
        var profissional = await GetProfissionalAtual(ct); if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de guardar favoritos." });
        var ids = (request.AlimentoIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        if (ids.Length > 60) return BadRequest(new { message = "Use ate 60 favoritos por refeicao." });
        var validos = await db.Alimentos.CountAsync(x => ids.Contains(x.Id) && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (validos != ids.Length) return BadRequest(new { message = "Um ou mais alimentos nao existem nesta organizacao." });
        var atuais = await db.AlimentosFavoritos.Where(x => x.ProfissionalId == profissional.Id && x.Refeicao == refeicao).ToListAsync(ct);
        db.AlimentosFavoritos.RemoveRange(atuais.Where(x => !ids.Contains(x.AlimentoId)));
        var manter = atuais.Select(x => x.AlimentoId).ToHashSet();
        foreach (var id in ids.Where(x => !manter.Contains(x))) db.AlimentosFavoritos.Add(new AlimentoFavorito { ProfissionalId = profissional.Id, Refeicao = refeicao, AlimentoId = id });
        await db.SaveChangesAsync(ct);
        return await GetFavoritos(ct);
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private sealed record AlimentoBase(string Nome, string Categoria, decimal Kcal, decimal Prot, decimal Carb, decimal Gord, decimal Fibra, string? Medida, decimal? GramasMedida, decimal? GramasPorMl = null);

    // Valores por 100 g. Alimentos in natura pela TACO (Unicamp) e USDA; whey pelos rotulos das marcas (variam um pouco por sabor).
    private static readonly AlimentoBase[] CatalogoBase =
    [
        new("Tapioca (goma hidratada)", "Cereais e derivados", 240m, 0m, 60m, 0m, 0m, "colher de sopa", 15m),
        new("Café preto (sem açúcar)", "Bebidas", 9m, 0.7m, 1.5m, 0.1m, 0m, "xícara de café", 50m, 1m),
        new("Maçã Fuji (com casca)", "Frutas", 56m, 0.3m, 15.2m, 0m, 1.3m, "unidade média", 130m),
        new("Canela em pó", "Temperos", 247m, 4m, 80.6m, 1.2m, 53.1m, "colher de chá", 2.6m),
        new("Whey protein concentrado (genérico)", "Suplementos", 400m, 72m, 15m, 6m, 0m, "dose", 30m),
        new("Whey Growth Concentrado 80%", "Suplementos", 403m, 76.7m, 9.3m, 6.7m, 0m, "dose", 30m),
        new("Whey New Millen 100% Whey", "Suplementos", 407m, 70m, 20m, 5m, 0m, "dose", 30m),
        new("Whey Max Titanium Top Whey 3W", "Suplementos", 425m, 80m, 11m, 6.8m, 0m, "dose", 40m),
        new("Whey Integralmédica 100% Pure", "Suplementos", 407m, 70m, 17m, 6.3m, 0m, "dose", 30m),
    ];

    private static string? Validar(UpsertAlimentoRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return "Nome do alimento e obrigatorio.";
        if (r.CaloriasPor100g < 0 || r.ProteinasPor100g < 0 || r.CarboidratosPor100g < 0 || r.GordurasPor100g < 0 || r.FibrasPor100g < 0) return "Valores nutricionais nao podem ser negativos.";
        if (r.GramasPorUnidade is <= 0 || r.GramasPorMl is <= 0) return "Peso da medida e densidade precisam ser maiores que zero.";
        if (r.UnidadeNome is { Length: > 40 }) return "Nome da medida caseira pode ter ate 40 caracteres.";
        return null;
    }
    private void Auditar(string acao, Alimento item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao, Entidade = nameof(Alimento), EntidadeId = item.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private static object Snapshot(Alimento x) => new { x.Id, x.Nome, x.Categoria, x.CaloriasPor100g, x.ProteinasPor100g, x.CarboidratosPor100g, x.GordurasPor100g, x.FibrasPor100g, x.UnidadeNome, x.GramasPorUnidade, x.GramasPorMl, x.Ativo };
    private static AlimentoResponse ToResponse(Alimento x) => new(x.Id, x.Nome, x.Categoria, x.CaloriasPor100g, x.ProteinasPor100g, x.CarboidratosPor100g, x.GordurasPor100g, x.FibrasPor100g, x.Ativo, x.CreatedAtUtc, x.UpdatedAtUtc, x.UnidadeNome, x.GramasPorUnidade, x.GramasPorMl);
    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static string Normalizar(string valor) { var d = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD); var sb = new StringBuilder(); foreach (var c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c); return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries)); }
}
