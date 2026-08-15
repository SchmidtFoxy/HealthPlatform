using System.Text.Json;
using HealthPlatform.Api.Contracts.Metas;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class MetasController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/metas")]
    public async Task<ActionResult<IReadOnlyCollection<MetaPacienteResponse>>> GetAll(Guid pacienteId, [FromQuery] bool incluirEncerradas = false, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = QueryCompleta().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        if (!incluirEncerradas) query = query.Where(x => x.Status == "Ativa");
        var itens = await query.OrderBy(x => x.Nome).ToListAsync(ct);
        return Ok(itens.Select(x => ToResponse(x, hoje)).ToList());
    }

    [HttpGet("api/metas/{id:guid}")]
    public async Task<ActionResult<MetaPacienteResponse>> GetById(Guid id, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await QueryCompleta().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Meta nao encontrada." }) : Ok(ToResponse(item, hoje));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/metas")]
    public async Task<ActionResult<MetaPacienteResponse>> Create(Guid pacienteId, UpsertMetaRequest request, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return BadRequest(new { message = "Configure o perfil profissional antes de criar metas." });
        var erro = await Validar(pacienteId, request, ct); if (erro is not null) return BadRequest(new { message = erro });
        var item = new MetaPaciente { PacienteId = pacienteId, ProfissionalId = profissional.Id, Nome = request.Nome.Trim(), Tipo = NormalizarTipo(request.Tipo), ValorObjetivo = request.ValorObjetivo, Unidade = Limpar(request.Unidade), Frequencia = NormalizarFrequencia(request.Frequencia), DataInicio = request.DataInicio, DataFim = request.DataFim, Status = "Ativa", Observacoes = Limpar(request.Observacoes) };
        db.MetasPaciente.Add(item); Auditar("CREATE", item, null, Snapshot(item)); await db.SaveChangesAsync(ct);
        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(criado, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPut("api/metas/{id:guid}")]
    public async Task<ActionResult<MetaPacienteResponse>> Update(Guid id, UpsertMetaRequest request, CancellationToken ct)
    {
        var item = await db.MetasPaciente.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Meta nao encontrada." });
        var erro = await Validar(item.PacienteId, request, ct); if (erro is not null) return BadRequest(new { message = erro });
        var antes = Snapshot(item); item.Nome = request.Nome.Trim(); item.Tipo = NormalizarTipo(request.Tipo); item.ValorObjetivo = request.ValorObjetivo; item.Unidade = Limpar(request.Unidade); item.Frequencia = NormalizarFrequencia(request.Frequencia); item.DataInicio = request.DataInicio; item.DataFim = request.DataFim; item.Observacoes = Limpar(request.Observacoes);
        Auditar("UPDATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct); return Ok(ToResponse(atualizado, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("api/metas/{id:guid}/registros")]
    public async Task<ActionResult<RegistroMetaResponse>> Registrar(Guid id, RegistrarMetaRequest request, CancellationToken ct)
    {
        var meta = await db.MetasPaciente.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (meta is null) return NotFound(new { message = "Meta nao encontrada." });
        if (request.Data < meta.DataInicio || (meta.DataFim.HasValue && request.Data > meta.DataFim.Value)) return BadRequest(new { message = "Data do registro esta fora do periodo da meta." });
        if (request.Valor is null && request.Concluida is null) return BadRequest(new { message = "Informe valor ou concluida." });
        var registro = await db.RegistrosMetas.FirstOrDefaultAsync(x => x.MetaPacienteId == id && x.Data == request.Data, ct);
        object? antes = registro is null ? null : new { registro.Valor, registro.Concluida, registro.Observacao };
        if (registro is null) { registro = new RegistroMeta { MetaPacienteId = id, Data = request.Data }; db.RegistrosMetas.Add(registro); }
        registro.Valor = request.Valor; registro.Concluida = request.Concluida ?? CalcularConcluida(meta, request.Valor); registro.Observacao = Limpar(request.Observacao);
        AuditarRegistro(registro, antes); await db.SaveChangesAsync(ct); return Ok(ToRegistroResponse(registro));
    }

    [HttpGet("api/metas/{id:guid}/registros")]
    public async Task<ActionResult<IReadOnlyCollection<RegistroMetaResponse>>> GetRegistros(Guid id, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct)
    {
        var existe = await db.MetasPaciente.AnyAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct); if (!existe) return NotFound(new { message = "Meta nao encontrada." });
        var q = db.RegistrosMetas.AsNoTracking().Where(x => x.MetaPacienteId == id); if (inicio.HasValue) q = q.Where(x => x.Data >= inicio.Value); if (fim.HasValue) q = q.Where(x => x.Data <= fim.Value);
        return Ok(await q.OrderByDescending(x => x.Data).Select(x => ToRegistroResponse(x)).ToListAsync(ct));
    }

    [HttpPost("api/metas/{id:guid}/status/{status}")]
    public async Task<ActionResult<MetaPacienteResponse>> SetStatus(Guid id, string status, CancellationToken ct)
    {
        var item = await db.MetasPaciente.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct); if (item is null) return NotFound(new { message = "Meta nao encontrada." });
        var novo = NormalizarStatus(status); if (novo is not ("Ativa" or "Pausada" or "Concluida" or "Cancelada")) return BadRequest(new { message = "Status permitido: Ativa, Pausada, Concluida ou Cancelada." });
        var antes = new { item.Status }; item.Status = novo; Auditar("STATUS", item, antes, new { item.Status }); await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == id, ct); return Ok(ToResponse(atualizado, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    private IQueryable<MetaPaciente> QueryCompleta() => db.MetasPaciente.AsNoTracking().Include(x => x.Paciente).Include(x => x.Profissional).Include(x => x.Registros);
    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) => await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<string?> Validar(Guid pacienteId, UpsertMetaRequest r, CancellationToken ct) { if (!await PacienteExiste(pacienteId, ct)) return "Paciente nao encontrado ou inativo."; if (string.IsNullOrWhiteSpace(r.Nome)) return "Nome da meta e obrigatorio."; if (r.ValorObjetivo.HasValue && r.ValorObjetivo <= 0) return "Valor objetivo deve ser maior que zero."; if (r.DataFim.HasValue && r.DataFim < r.DataInicio) return "Data final nao pode ser anterior a data inicial."; if (NormalizarFrequencia(r.Frequencia) is not ("Diaria" or "Semanal" or "Mensal")) return "Frequencia permitida: Diaria, Semanal ou Mensal."; return null; }
    private static bool? CalcularConcluida(MetaPaciente m, decimal? valor) => m.ValorObjetivo.HasValue && valor.HasValue ? valor.Value >= m.ValorObjetivo.Value : null;
    private static decimal? Progresso(MetaPaciente m, RegistroMeta? r) { if (r?.Concluida == true) return 100; if (!m.ValorObjetivo.HasValue || r is null || !r.Valor.HasValue) return null; return Math.Round(Math.Clamp(r.Valor.Value / m.ValorObjetivo.Value * 100m, 0m, 100m), 1); }
    private static MetaPacienteResponse ToResponse(MetaPaciente x, DateOnly hoje) { var r = x.Registros.FirstOrDefault(z => z.Data == hoje); return new(x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.Nome, x.Tipo, x.ValorObjetivo, x.Unidade, x.Frequencia, x.DataInicio, x.DataFim, x.Status, x.Observacoes, Progresso(x, r), r is null ? null : ToRegistroResponse(r), x.CreatedAtUtc, x.UpdatedAtUtc); }
    private static RegistroMetaResponse ToRegistroResponse(RegistroMeta x) => new(x.Id, x.Data, x.Valor, x.Concluida, x.Observacao, x.CreatedAtUtc);
    private void Auditar(string acao, MetaPaciente item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao, Entidade = nameof(MetaPaciente), EntidadeId = item.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private void AuditarRegistro(RegistroMeta r, object? antes) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = antes is null ? "CREATE" : "UPDATE", Entidade = nameof(RegistroMeta), EntidadeId = r.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = JsonSerializer.Serialize(new { r.MetaPacienteId, r.Data, r.Valor, r.Concluida, r.Observacao }), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private static object Snapshot(MetaPaciente x) => new { x.Id, x.PacienteId, x.ProfissionalId, x.Nome, x.Tipo, x.ValorObjetivo, x.Unidade, x.Frequencia, x.DataInicio, x.DataFim, x.Status, x.Observacoes };
    private static string NormalizarTipo(string? s) => string.IsNullOrWhiteSpace(s) ? "Habito" : s.Trim();
    private static string NormalizarFrequencia(string? s) => NormalizarPalavra(s, "Diaria");
    private static string NormalizarStatus(string? s) => NormalizarPalavra(s, "Ativa");
    private static string NormalizarPalavra(string? s, string padrao) => string.IsNullOrWhiteSpace(s) ? padrao : char.ToUpperInvariant(s.Trim()[0]) + s.Trim()[1..].ToLowerInvariant();
    private static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
