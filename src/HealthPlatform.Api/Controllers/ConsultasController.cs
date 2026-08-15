using System.Text.Json;
using HealthPlatform.Api.Contracts.Consultas;
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
public class ConsultasController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/consultas")]
    public async Task<ActionResult<IReadOnlyCollection<ConsultaResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new ConsultaResponse(
                x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.DataHoraUtc,
                x.Motivo, x.QueixaPrincipal, x.Evolucao, x.Conduta, x.Orientacoes,
                x.Status.ToString(), x.Avaliacao != null, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(itens);
    }

    [HttpGet("api/consultas/{id:guid}")]
    public async Task<ActionResult<ConsultaResponse>> GetById(Guid id, CancellationToken ct)
    {
        var consulta = await db.Consultas.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Avaliacao)
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        return consulta is null ? NotFound(new { message = "Consulta nao encontrada." }) : Ok(ToResponse(consulta));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/consultas")]
    public async Task<ActionResult<ConsultaResponse>> Create(Guid pacienteId, CreateConsultaRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Cadastre seu perfil profissional em PUT /api/profissionais/me antes de criar consultas." });

        if (!TryParseStatus(request.Status, out var status))
            return BadRequest(new { message = "Status invalido. Use Agendada, Confirmada, Realizada, Cancelada ou Faltou." });

        var consulta = new Consulta
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            DataHoraUtc = request.DataHoraUtc == default ? DateTime.UtcNow : request.DataHoraUtc.ToUniversalTime(),
            Motivo = Normalizar(request.Motivo),
            QueixaPrincipal = Normalizar(request.QueixaPrincipal),
            Evolucao = Normalizar(request.Evolucao),
            Conduta = Normalizar(request.Conduta),
            Orientacoes = Normalizar(request.Orientacoes),
            Status = status
        };

        db.Consultas.Add(consulta);
        AdicionarAuditoria("CREATE", consulta, null, Snapshot(consulta));
        await db.SaveChangesAsync(ct);

        consulta.Profissional = profissional;
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, ToResponse(consulta));
    }

    [HttpPut("api/consultas/{id:guid}")]
    public async Task<ActionResult<ConsultaResponse>> Update(Guid id, UpdateConsultaRequest request, CancellationToken ct)
    {
        var consulta = await db.Consultas
            .Include(x => x.Profissional)
            .Include(x => x.Avaliacao)
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (consulta is null)
            return NotFound(new { message = "Consulta nao encontrada." });

        var profissionalAtual = await GetProfissionalAtual(ct);
        if (profissionalAtual is null || consulta.ProfissionalId != profissionalAtual.Id)
            return Forbid();

        if (!TryParseStatus(request.Status, out var status))
            return BadRequest(new { message = "Status invalido. Use Agendada, Confirmada, Realizada, Cancelada ou Faltou." });

        var antes = Snapshot(consulta);
        consulta.DataHoraUtc = request.DataHoraUtc == default ? consulta.DataHoraUtc : request.DataHoraUtc.ToUniversalTime();
        consulta.Motivo = Normalizar(request.Motivo);
        consulta.QueixaPrincipal = Normalizar(request.QueixaPrincipal);
        consulta.Evolucao = Normalizar(request.Evolucao);
        consulta.Conduta = Normalizar(request.Conduta);
        consulta.Orientacoes = Normalizar(request.Orientacoes);
        consulta.Status = status;

        AdicionarAuditoria("UPDATE", consulta, antes, Snapshot(consulta));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(consulta));
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private static bool TryParseStatus(string? value, out StatusConsulta status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = StatusConsulta.Agendada;
            return true;
        }
        return Enum.TryParse(value.Trim(), true, out status) && Enum.IsDefined(typeof(StatusConsulta), status);
    }

    private void AdicionarAuditoria(string acao, Consulta consulta, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Consulta),
            EntidadeId = consulta.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static object Snapshot(Consulta x) => new
    {
        x.Id,
        x.PacienteId,
        x.ProfissionalId,
        x.DataHoraUtc,
        x.Motivo,
        x.QueixaPrincipal,
        x.Evolucao,
        x.Conduta,
        x.Orientacoes,
        x.Status
    };

    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ConsultaResponse ToResponse(Consulta x) => new(
        x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.DataHoraUtc,
        x.Motivo, x.QueixaPrincipal, x.Evolucao, x.Conduta, x.Orientacoes,
        x.Status.ToString(), x.Avaliacao is not null, x.CreatedAtUtc, x.UpdatedAtUtc);
}
