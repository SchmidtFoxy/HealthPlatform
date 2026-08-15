using System.Text.Json;
using HealthPlatform.Api.Contracts.PerguntasAnamnese;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/anamnese/perguntas")]
public class PerguntasAnamneseController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly string[] TiposPermitidos = ["Texto", "Numero", "SimNao", "Escala", "Opcao"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PerguntaAnamneseResponse>>> Get(
        [FromQuery] bool incluirInativas = false,
        CancellationToken ct = default)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Cadastre seu perfil profissional antes de configurar a anamnese." });

        var itens = await db.PerguntasAnamnese.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.ProfissionalId == profissional.Id && (incluirInativas || x.Ativa))
            .OrderBy(x => x.Ordem).ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PerguntaAnamneseResponse>> Create(CreatePerguntaAnamneseRequest request, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Cadastre seu perfil profissional antes de configurar a anamnese." });

        var texto = request.Texto?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest(new { message = "Texto da pergunta e obrigatorio." });
        if (texto.Length > 500)
            return BadRequest(new { message = "Texto da pergunta deve possuir no maximo 500 caracteres." });

        var tipo = NormalizarTipo(request.TipoResposta);
        if (tipo is null)
            return BadRequest(new { message = $"Tipo de resposta invalido. Use: {string.Join(", ", TiposPermitidos)}." });

        var opcoes = request.Opcoes?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray() ?? [];
        if (tipo == "Opcao" && opcoes.Length < 2)
            return BadRequest(new { message = "Perguntas do tipo Opcao precisam de pelo menos duas opcoes." });

        var proximaOrdem = request.Ordem ?? ((await db.PerguntasAnamnese
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.ProfissionalId == profissional.Id)
            .MaxAsync(x => (int?)x.Ordem, ct) ?? 0) + 1);

        var item = new PerguntaAnamnese
        {
            OrganizacaoId = currentUser.OrganizationId,
            ProfissionalId = profissional.Id,
            Texto = texto,
            TipoResposta = tipo,
            OpcoesJson = opcoes.Length == 0 ? null : JsonSerializer.Serialize(opcoes),
            Ordem = Math.Max(0, proximaOrdem),
            Ativa = true
        };

        db.PerguntasAnamnese.Add(item);
        AdicionarAuditoria("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Created($"/api/anamnese/perguntas/{item.Id}", ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PerguntaAnamneseResponse>> Update(Guid id, CreatePerguntaAnamneseRequest request, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var item = await db.PerguntasAnamnese.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.ProfissionalId == profissional.Id, ct);
        if (item is null) return NotFound(new { message = "Pergunta nao encontrada." });

        var texto = request.Texto?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest(new { message = "Texto da pergunta e obrigatorio." });
        if (texto.Length > 500)
            return BadRequest(new { message = "Texto da pergunta deve possuir no maximo 500 caracteres." });

        var tipo = NormalizarTipo(request.TipoResposta);
        if (tipo is null)
            return BadRequest(new { message = $"Tipo de resposta invalido. Use: {string.Join(", ", TiposPermitidos)}." });

        var opcoes = request.Opcoes?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray() ?? [];
        if (tipo == "Opcao" && opcoes.Length < 2)
            return BadRequest(new { message = "Perguntas do tipo Opcao precisam de pelo menos duas opcoes." });

        var antes = Snapshot(item);
        item.Texto = texto;
        item.TipoResposta = tipo;
        item.OpcoesJson = opcoes.Length == 0 ? null : JsonSerializer.Serialize(opcoes);
        if (request.Ordem.HasValue) item.Ordem = Math.Max(0, request.Ordem.Value);

        AdicionarAuditoria("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var item = await db.PerguntasAnamnese.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.ProfissionalId == profissional.Id, ct);
        if (item is null) return NotFound(new { message = "Pergunta nao encontrada." });
        if (!item.Ativa) return NoContent();

        var antes = Snapshot(item);
        item.Ativa = false;
        AdicionarAuditoria("DEACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }


    [HttpPost("{id:guid}/reativar")]
    public async Task<ActionResult<PerguntaAnamneseResponse>> Reactivate(Guid id, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var item = await db.PerguntasAnamnese.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.ProfissionalId == profissional.Id, ct);

        if (item is null)
            return NotFound(new { message = "Pergunta nao encontrada." });

        var antes = Snapshot(item);
        item.Ativa = true;
        item.UpdatedAtUtc = DateTime.UtcNow;

        AdicionarAuditoria("ACTIVATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);

        return Ok(ToResponse(item));
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private static string? NormalizarTipo(string? tipo)
    {
        var valor = string.IsNullOrWhiteSpace(tipo) ? "Texto" : tipo.Trim();
        return TiposPermitidos.FirstOrDefault(x => x.Equals(valor, StringComparison.OrdinalIgnoreCase));
    }

    private static PerguntaAnamneseResponse ToResponse(PerguntaAnamnese x)
    {
        string[] opcoes = [];
        if (!string.IsNullOrWhiteSpace(x.OpcoesJson))
        {
            try { opcoes = JsonSerializer.Deserialize<string[]>(x.OpcoesJson) ?? []; } catch { }
        }
        return new(x.Id, x.Texto, x.TipoResposta, opcoes, x.Ordem, x.Ativa, x.CreatedAtUtc);
    }

    private void AdicionarAuditoria(string acao, PerguntaAnamnese item, object? antes, object? depois) =>
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(PerguntaAnamnese),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static object Snapshot(PerguntaAnamnese x) => new { x.Id, x.Texto, x.TipoResposta, x.OpcoesJson, x.Ordem, x.Ativa };
}
