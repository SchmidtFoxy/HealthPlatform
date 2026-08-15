using System.Text.Json;
using HealthPlatform.Api.Contracts.Diario;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class DiarioPacienteController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/diario")]
    public async Task<ActionResult<IReadOnlyCollection<RegistroDiarioResponse>>> GetAll(Guid pacienteId, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] string? tipo, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var q = db.RegistrosDiarioPaciente.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        if (inicio.HasValue) { var d = inicio.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); q = q.Where(x => x.DataHoraUtc >= d); }
        if (fim.HasValue) { var d = fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); q = q.Where(x => x.DataHoraUtc < d); }
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(x => x.Tipo == NormalizarTipo(tipo));
        return Ok(await q.OrderByDescending(x => x.DataHoraUtc).Take(500).Select(x => ToResponse(x)).ToListAsync(ct));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/diario")]
    public async Task<ActionResult<RegistroDiarioResponse>> Create(Guid pacienteId, UpsertRegistroDiarioRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var item = new RegistroDiarioPaciente { PacienteId = pacienteId, DataHoraUtc = request.DataHoraUtc.ToUniversalTime(), Tipo = NormalizarTipo(request.Tipo), Descricao = Limpar(request.Descricao), ValorNumerico = request.ValorNumerico, Unidade = Limpar(request.Unidade), Escala = request.Escala, ImagemUrl = Limpar(request.ImagemUrl) };
        db.RegistrosDiarioPaciente.Add(item); Auditar("CREATE", item, null, Snapshot(item)); await db.SaveChangesAsync(ct); return Created($"/api/diario/{item.Id}", ToResponse(item));
    }

    [HttpPut("api/diario/{id:guid}")]
    public async Task<ActionResult<RegistroDiarioResponse>> Update(Guid id, UpsertRegistroDiarioRequest request, CancellationToken ct)
    {
        var item = await db.RegistrosDiarioPaciente.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct); if (item is null) return NotFound(new { message = "Registro de diario nao encontrado." });
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro }); var antes = Snapshot(item);
        item.DataHoraUtc = request.DataHoraUtc.ToUniversalTime(); item.Tipo = NormalizarTipo(request.Tipo); item.Descricao = Limpar(request.Descricao); item.ValorNumerico = request.ValorNumerico; item.Unidade = Limpar(request.Unidade); item.Escala = request.Escala; item.ImagemUrl = Limpar(request.ImagemUrl);
        Auditar("UPDATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [HttpDelete("api/diario/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.RegistrosDiarioPaciente.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct); if (item is null) return NotFound(new { message = "Registro de diario nao encontrado." });
        Auditar("DELETE", item, Snapshot(item), null); db.RegistrosDiarioPaciente.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/resumo-dia")]
    public async Task<ActionResult<ResumoDiaPacienteResponse>> ResumoDia(Guid pacienteId, [FromQuery] DateOnly? data, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow); var inicio = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var fim = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var metas = await db.MetasPaciente.AsNoTracking().Include(x => x.Registros.Where(r => r.Data == dia)).Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.Status == "Ativa" && x.DataInicio <= dia && (!x.DataFim.HasValue || x.DataFim >= dia)).OrderBy(x => x.Nome).ToListAsync(ct);
        var regs = await db.RegistrosDiarioPaciente.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.DataHoraUtc >= inicio && x.DataHoraUtc < fim).OrderByDescending(x => x.DataHoraUtc).ToListAsync(ct);
        var resumoMetas = metas.Select(m => { var r = m.Registros.FirstOrDefault(); decimal? progresso = r?.Concluida == true ? 100 : (m.ValorObjetivo.HasValue && r?.Valor.HasValue == true ? Math.Round(Math.Clamp(r.Valor.Value / m.ValorObjetivo.Value * 100m, 0m, 100m), 1) : null); return new ResumoMetaHojeResponse(m.Id, m.Nome, m.Tipo, m.ValorObjetivo, m.Unidade, r?.Valor, r?.Concluida, progresso); }).ToList();
        var concluidas = resumoMetas.Count(x => x.Concluida == true || x.ProgressoPercentual >= 100); var percentual = resumoMetas.Count == 0 ? 0 : Math.Round(concluidas * 100m / resumoMetas.Count, 1);
        return Ok(new ResumoDiaPacienteResponse(dia, resumoMetas, regs.Select(ToResponse).ToList(), resumoMetas.Count, concluidas, percentual));
    }

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private static string? Validar(UpsertRegistroDiarioRequest r) { if (string.IsNullOrWhiteSpace(r.Tipo)) return "Tipo do registro e obrigatorio."; if (r.Escala.HasValue && (r.Escala < 0 || r.Escala > 10)) return "Escala deve estar entre 0 e 10."; if (string.IsNullOrWhiteSpace(r.Descricao) && !r.ValorNumerico.HasValue && !r.Escala.HasValue && string.IsNullOrWhiteSpace(r.ImagemUrl)) return "Informe ao menos descricao, valor, escala ou imagem."; return null; }
    private void Auditar(string acao, RegistroDiarioPaciente item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao, Entidade = nameof(RegistroDiarioPaciente), EntidadeId = item.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private static object Snapshot(RegistroDiarioPaciente x) => new { x.Id, x.PacienteId, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala, x.ImagemUrl };
    private static RegistroDiarioResponse ToResponse(RegistroDiarioPaciente x) => new(x.Id, x.PacienteId, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala, x.ImagemUrl, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static string NormalizarTipo(string? s) => string.IsNullOrWhiteSpace(s) ? "Observacao" : s.Trim();
    private static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
