using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record SalvarMedicamentoRequest(string Nome, decimal? Dose, string? Unidade, string? Via, string? Frequencia, string? HorariosLocais, DateOnly DataInicio, DateOnly? DataFim, string? Orientacao, bool Ativo = true);
public sealed record RegistrarTomadaMedicamentoRequest(DateTime DataHoraPrevistaUtc, string? Status, string? Observacao);

[ApiController]
public sealed class MedicamentosController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly HashSet<string> StatusPermitidos = new(StringComparer.OrdinalIgnoreCase) { "Tomado", "Pulado" };

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/medicamentos")]
    public async Task<IActionResult> Listar(Guid pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        if (await ProfissionalAtual(ct) is null) return Forbid();
        dias = Math.Clamp(dias, 1, 90);
        var desde = DateTime.UtcNow.AddDays(-dias);
        var medicamentos = await db.MedicamentosPaciente.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.Ativo).ThenBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Dose, x.Unidade, x.Via, x.Frequencia, x.HorariosLocais, x.DataInicio, x.DataFim, x.Orientacao, x.Ativo, profissionalNome = x.Profissional.Nome })
            .ToListAsync(ct);
        var registros = await db.RegistrosMedicamentos.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId && x.DataHoraPrevistaUtc >= desde)
            .OrderByDescending(x => x.DataHoraPrevistaUtc)
            .Select(x => new { x.Id, x.MedicamentoId, medicamentoNome = x.Medicamento.Nome, x.DataHoraPrevistaUtc, x.DataHoraTomadaUtc, x.Status, x.Observacao })
            .ToListAsync(ct);
        var total = registros.Count;
        var tomados = registros.Count(x => x.Status == "Tomado");
        return Ok(new { medicamentos, registros, aderencia = new { dias, total, tomados, percentual = total == 0 ? (int?)null : (int)Math.Round(tomados * 100m / total) } });
    }

    [Authorize]
    [HttpPost("api/pacientes/{pacienteId:guid}/medicamentos")]
    public async Task<IActionResult> Criar(Guid pacienteId, SalvarMedicamentoRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var profissional = await ProfissionalAtual(ct); if (profissional is null) return Forbid();
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var item = new MedicamentoPaciente { OrganizacaoId=currentUser.OrganizationId, PacienteId=pacienteId, ProfissionalId=profissional.Id };
        Aplicar(item, request); db.MedicamentosPaciente.Add(item); Auditar("CREATE", nameof(MedicamentoPaciente), item.Id, null, Snapshot(item));
        await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpPut("api/medicamentos/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, SalvarMedicamentoRequest request, CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return Forbid();
        var item = await db.MedicamentosPaciente.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound();
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var antes = Snapshot(item); Aplicar(item, request); Auditar("UPDATE", nameof(MedicamentoPaciente), item.Id, antes, Snapshot(item));
        await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpDelete("api/medicamentos/{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return Forbid();
        var item = await db.MedicamentosPaciente.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(); var antes = Snapshot(item); item.Ativo = false;
        Auditar("DEACTIVATE", nameof(MedicamentoPaciente), item.Id, antes, Snapshot(item)); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/medicamentos")]
    public async Task<IActionResult> MeusMedicamentos([FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        var paciente = await MeuPaciente(ct); if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        offsetMinutos = Math.Clamp(offsetMinutos, -840, 840);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(offsetMinutos));
        var inicioUtc = DateTime.SpecifyKind(hoje.ToDateTime(TimeOnly.MinValue).AddMinutes(-offsetMinutos), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);
        var medicamentos = await db.MedicamentosPaciente.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == paciente.Id && x.Ativo && x.DataInicio <= hoje && (!x.DataFim.HasValue || x.DataFim >= hoje))
            .OrderBy(x => x.Nome).Select(x => new { x.Id, x.Nome, x.Dose, x.Unidade, x.Via, x.Frequencia, x.HorariosLocais, x.Orientacao, x.DataInicio, x.DataFim }).ToListAsync(ct);
        var registrosHoje = await db.RegistrosMedicamentos.AsNoTracking().Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == paciente.Id && x.DataHoraPrevistaUtc >= inicioUtc && x.DataHoraPrevistaUtc < fimUtc)
            .Select(x => new { x.Id, x.MedicamentoId, x.DataHoraPrevistaUtc, x.DataHoraTomadaUtc, x.Status, x.Observacao }).ToListAsync(ct);
        return Ok(new { data = hoje, medicamentos, registrosHoje });
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/portal/me/medicamentos/{medicamentoId:guid}/tomadas")]
    public async Task<IActionResult> RegistrarTomada(Guid medicamentoId, RegistrarTomadaMedicamentoRequest request, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct); if (paciente is null) return NotFound();
        var medicamento = await db.MedicamentosPaciente.AsNoTracking().FirstOrDefaultAsync(x => x.Id == medicamentoId && x.PacienteId == paciente.Id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (medicamento is null) return NotFound(new { message = "Medicamento ativo nao encontrado." });
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Tomado" : request.Status.Trim();
        if (!StatusPermitidos.Contains(status)) return BadRequest(new { message = "Status deve ser Tomado ou Pulado." });
        var prevista = request.DataHoraPrevistaUtc.ToUniversalTime();
        var existente = await db.RegistrosMedicamentos.FirstOrDefaultAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == paciente.Id && x.MedicamentoId == medicamentoId && x.DataHoraPrevistaUtc == prevista, ct);
        object? antes = existente is null ? null : Snapshot(existente);
        if (existente is null) { existente = new RegistroMedicamento { OrganizacaoId=currentUser.OrganizationId, PacienteId=paciente.Id, MedicamentoId=medicamentoId, DataHoraPrevistaUtc=prevista }; db.RegistrosMedicamentos.Add(existente); }
        existente.Status = status; existente.DataHoraTomadaUtc = status == "Tomado" ? DateTime.UtcNow : null; existente.Observacao = Limpar(request.Observacao);
        Auditar(antes is null ? "CREATE" : "UPDATE", nameof(RegistroMedicamento), existente.Id, antes, Snapshot(existente)); await db.SaveChangesAsync(ct);
        return Ok(new { existente.Id, existente.MedicamentoId, existente.DataHoraPrevistaUtc, existente.DataHoraTomadaUtc, existente.Status, existente.Observacao });
    }

    private async Task<Paciente?> MeuPaciente(CancellationToken ct) => await db.Pacientes.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AsNoTracking().AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct) => await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private static string? Validar(SalvarMedicamentoRequest r) { if (string.IsNullOrWhiteSpace(r.Nome) || r.Nome.Trim().Length > 220) return "Nome do medicamento invalido."; if (r.Dose is < 0) return "Dose invalida."; if ((r.Orientacao?.Length ?? 0) > 1600) return "Orientacao excede 1600 caracteres."; if (r.DataFim.HasValue && r.DataFim < r.DataInicio) return "Data final anterior a data inicial."; return null; }
    private static void Aplicar(MedicamentoPaciente x, SalvarMedicamentoRequest r) { x.Nome=r.Nome.Trim(); x.Dose=r.Dose; x.Unidade=Limpar(r.Unidade); x.Via=Limpar(r.Via); x.Frequencia=Limpar(r.Frequencia)??"Diario"; x.HorariosLocais=NormalizarHorarios(r.HorariosLocais); x.DataInicio=r.DataInicio; x.DataFim=r.DataFim; x.Orientacao=Limpar(r.Orientacao); x.Ativo=r.Ativo; }
    private static string? NormalizarHorarios(string? value) { var items=(value??"").Split(new[]{',',';',' '},StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Where(x=>TimeOnly.TryParse(x,out _)).Distinct().OrderBy(x=>x).ToArray(); return items.Length==0?null:string.Join(',',items); }
    private static string? Limpar(string? value) => string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static object ToResponse(MedicamentoPaciente x) => new { x.Id,x.PacienteId,x.ProfissionalId,x.Nome,x.Dose,x.Unidade,x.Via,x.Frequencia,x.HorariosLocais,x.DataInicio,x.DataFim,x.Orientacao,x.Ativo,x.CreatedAtUtc,x.UpdatedAtUtc };
    private static object Snapshot(MedicamentoPaciente x) => ToResponse(x);
    private static object Snapshot(RegistroMedicamento x) => new { x.Id,x.PacienteId,x.MedicamentoId,x.DataHoraPrevistaUtc,x.DataHoraTomadaUtc,x.Status,x.Observacao };
    private void Auditar(string acao,string entidade,Guid id,object? antes,object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId=currentUser.OrganizationId, UsuarioId=currentUser.UserId, Acao=acao, Entidade=entidade, EntidadeId=id.ToString(), DadosAnterioresJson=antes is null?null:JsonSerializer.Serialize(antes), DadosNovosJson=depois is null?null:JsonSerializer.Serialize(depois), IpAddress=httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
}
