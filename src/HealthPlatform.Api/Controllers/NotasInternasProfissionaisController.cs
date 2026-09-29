using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Medico,Nutricionista,Personal,Secretaria")]
[Route("api/pacientes/{pacienteId:guid}/notas-internas")]
public class NotasInternasProfissionaisController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public record CriarNotaInternaRequest(string Conteudo, string? Categoria);
    public record AtualizarNotaInternaRequest(string Conteudo, string? Categoria);
    public record AlterarFixacaoNotaRequest(bool Fixada);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid pacienteId, [FromQuery] bool incluirArquivadas = false, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var query = db.NotasInternasProfissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId);
        if (!incluirArquivadas) query = query.Where(x => !x.Arquivada);
        var itens = await query.OrderByDescending(x => x.Fixada).ThenByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new { x.Id, x.PacienteId, x.AutorUsuarioId, x.AutorNome, x.Categoria, x.Conteudo, x.Fixada, x.Arquivada, x.CreatedAtUtc, x.UpdatedAtUtc })
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Guid pacienteId, CriarNotaInternaRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var conteudo = LimparConteudo(request.Conteudo);
        if (conteudo is null) return BadRequest(new { message = "Informe o conteudo da nota interna." });
        var autor = await db.Users.AsNoTracking().Where(x => x.Id == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome).FirstOrDefaultAsync(ct) ?? "Profissional";
        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = NormalizarCategoria(request.Categoria),
            Conteudo = conteudo
        };
        db.NotasInternasProfissionais.Add(nota);
        Auditar("INTERNAL_NOTE_CREATED", nota, null, Snapshot(nota));
        await db.SaveChangesAsync(ct);
        return Created($"/api/pacientes/{pacienteId}/notas-internas/{nota.Id}", nota);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid pacienteId, Guid id, AtualizarNotaInternaRequest request, CancellationToken ct)
    {
        var nota = await Obter(pacienteId, id, ct);
        if (nota is null) return NotFound(new { message = "Nota interna nao encontrada." });
        var conteudo = LimparConteudo(request.Conteudo);
        if (conteudo is null) return BadRequest(new { message = "Informe o conteudo da nota interna." });
        var antes = Snapshot(nota);
        nota.Conteudo = conteudo;
        nota.Categoria = NormalizarCategoria(request.Categoria);
        nota.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("INTERNAL_NOTE_UPDATED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(ct);
        return Ok(nota);
    }

    [HttpPatch("{id:guid}/fixacao")]
    public async Task<IActionResult> AlterarFixacao(Guid pacienteId, Guid id, AlterarFixacaoNotaRequest request, CancellationToken ct)
    {
        var nota = await Obter(pacienteId, id, ct);
        if (nota is null) return NotFound(new { message = "Nota interna nao encontrada." });
        var antes = Snapshot(nota);
        nota.Fixada = request.Fixada;
        nota.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("INTERNAL_NOTE_PIN_CHANGED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(ct);
        return Ok(nota);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Arquivar(Guid pacienteId, Guid id, CancellationToken ct)
    {
        var nota = await Obter(pacienteId, id, ct);
        if (nota is null) return NotFound(new { message = "Nota interna nao encontrada." });
        var antes = Snapshot(nota);
        nota.Arquivada = true;
        nota.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("INTERNAL_NOTE_ARCHIVED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) => db.Pacientes.AsNoTracking().AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);
    private Task<NotaInternaProfissional?> Obter(Guid pacienteId, Guid id, CancellationToken ct) => db.NotasInternasProfissionais.FirstOrDefaultAsync(x => x.Id == id && x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);
    private static string? LimparConteudo(string? value) { var v = value?.Trim(); return string.IsNullOrWhiteSpace(v) ? null : v.Length <= 4000 ? v : v[..4000]; }
    private static string NormalizarCategoria(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrWhiteSpace(v)) return "Geral";
        return v.Length <= 40 ? v : v[..40];
    }
    private static object Snapshot(NotaInternaProfissional x) => new { x.Id, x.PacienteId, x.AutorUsuarioId, x.AutorNome, x.Categoria, x.Conteudo, x.Fixada, x.Arquivada, x.CreatedAtUtc, x.UpdatedAtUtc };
    private void Auditar(string acao, NotaInternaProfissional nota, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId,
        UsuarioId = currentUser.UserId,
        Acao = acao,
        Entidade = nameof(NotaInternaProfissional),
        EntidadeId = nota.Id.ToString(),
        DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
        DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
        IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    });
}
