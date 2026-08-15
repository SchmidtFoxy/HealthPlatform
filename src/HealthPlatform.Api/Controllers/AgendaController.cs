using System.Text.Json;
using HealthPlatform.Api.Contracts.Agenda;
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
[Route("api/agenda")]
public class AgendaController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AgendaDiaResponse>> GetDia([FromQuery] DateOnly? data, [FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        if (!OffsetValido(offsetMinutos)) return BadRequest(new { message = "offsetMinutos deve estar entre -840 e 840." });
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de usar a agenda." });

        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(offsetMinutos));
        var (inicioUtc, fimUtc) = IntervaloUtc(dia, offsetMinutos);
        var consultas = await CarregarConsultas(profissional.Id, inicioUtc, fimUtc, offsetMinutos, ct);

        return Ok(new AgendaDiaResponse(
            dia,
            offsetMinutos,
            consultas.Count,
            consultas.Count(x => x.Status == StatusConsulta.Agendada.ToString()),
            consultas.Count(x => x.Status == StatusConsulta.Confirmada.ToString()),
            consultas.Count(x => x.Status == StatusConsulta.Realizada.ToString()),
            consultas.Count(x => x.Status == StatusConsulta.Cancelada.ToString()),
            consultas.Count(x => x.Status == StatusConsulta.Faltou.ToString()),
            consultas));
    }

    [HttpGet("periodo")]
    public async Task<ActionResult<IReadOnlyCollection<AgendaConsultaResponse>>> GetPeriodo(
        [FromQuery] DateOnly inicio,
        [FromQuery] DateOnly fim,
        [FromQuery] int offsetMinutos = 0,
        CancellationToken ct = default)
    {
        if (!OffsetValido(offsetMinutos)) return BadRequest(new { message = "offsetMinutos deve estar entre -840 e 840." });
        if (fim < inicio) return BadRequest(new { message = "A data final deve ser maior ou igual a inicial." });
        if (fim.DayNumber - inicio.DayNumber > 93) return BadRequest(new { message = "Consulte no maximo 93 dias por requisicao." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de usar a agenda." });
        var (inicioUtc, _) = IntervaloUtc(inicio, offsetMinutos);
        var (_, fimUtc) = IntervaloUtc(fim, offsetMinutos);
        return Ok(await CarregarConsultas(profissional.Id, inicioUtc, fimUtc, offsetMinutos, ct));
    }

    [HttpPatch("consultas/{id:guid}/status")]
    public async Task<ActionResult<AgendaConsultaResponse>> AlterarStatus(Guid id, AlterarStatusAgendaRequest request, [FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        if (!OffsetValido(offsetMinutos)) return BadRequest(new { message = "offsetMinutos deve estar entre -840 e 840." });
        if (!TryParseStatus(request.Status, out var status))
            return BadRequest(new { message = "Status invalido. Use Agendada, Confirmada, Realizada, Cancelada ou Faltou." });

        var consulta = await CarregarConsultaAtual(id, ct);
        if (consulta is null) return NotFound(new { message = "Consulta nao encontrada." });
        var antes = Snapshot(consulta);
        consulta.Status = status;
        AdicionarAuditoria("STATUS", consulta, antes, Snapshot(consulta));
        await db.SaveChangesAsync(ct);
        return Ok(ToAgendaResponse(consulta, offsetMinutos));
    }

    [HttpPatch("consultas/{id:guid}/reagendar")]
    public async Task<ActionResult<AgendaConsultaResponse>> Reagendar(Guid id, ReagendarConsultaRequest request, CancellationToken ct = default)
    {
        if (!OffsetValido(request.OffsetMinutos)) return BadRequest(new { message = "OffsetMinutos deve estar entre -840 e 840." });
        if (request.DataHoraLocal == default) return BadRequest(new { message = "DataHoraLocal e obrigatoria." });

        var consulta = await CarregarConsultaAtual(id, ct);
        if (consulta is null) return NotFound(new { message = "Consulta nao encontrada." });
        var antes = Snapshot(consulta);
        var localSemKind = DateTime.SpecifyKind(request.DataHoraLocal, DateTimeKind.Unspecified);
        consulta.DataHoraUtc = DateTime.SpecifyKind(localSemKind.AddMinutes(-request.OffsetMinutos), DateTimeKind.Utc);
        if (consulta.Status is StatusConsulta.Cancelada or StatusConsulta.Faltou)
            consulta.Status = StatusConsulta.Agendada;
        AdicionarAuditoria("RESCHEDULE", consulta, antes, Snapshot(consulta));
        await db.SaveChangesAsync(ct);
        return Ok(ToAgendaResponse(consulta, request.OffsetMinutos));
    }

    private async Task<List<AgendaConsultaResponse>> CarregarConsultas(Guid profissionalId, DateTime inicioUtc, DateTime fimUtc, int offsetMinutos, CancellationToken ct)
    {
        var entidades = await db.Consultas.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Avaliacao)
            .Include(x => x.Anamnese)
            .Where(x => x.ProfissionalId == profissionalId && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.DataHoraUtc >= inicioUtc && x.DataHoraUtc < fimUtc)
            .OrderBy(x => x.DataHoraUtc)
            .ToListAsync(ct);
        return entidades.Select(x => ToAgendaResponse(x, offsetMinutos)).ToList();
    }

    private async Task<Consulta?> CarregarConsultaAtual(Guid id, CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return null;
        return await db.Consultas
            .Include(x => x.Paciente)
            .Include(x => x.Avaliacao)
            .Include(x => x.Anamnese)
            .FirstOrDefaultAsync(x => x.Id == id && x.ProfissionalId == profissional.Id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private static AgendaConsultaResponse ToAgendaResponse(Consulta x, int offsetMinutos) => new(
        x.Id, x.PacienteId, x.Paciente.Nome, x.DataHoraUtc, ParaHorarioLocal(x.DataHoraUtc, offsetMinutos),
        x.Status.ToString(), x.Motivo, x.Paciente.Telefone, x.Paciente.Email, x.Avaliacao is not null, x.Anamnese is not null);

    private static DateTime ParaHorarioLocal(DateTime utc, int offsetMinutos) =>
        DateTime.SpecifyKind(utc.AddMinutes(offsetMinutos), DateTimeKind.Unspecified);

    private static (DateTime InicioUtc, DateTime FimUtc) IntervaloUtc(DateOnly data, int offsetMinutos)
    {
        var local = DateTime.SpecifyKind(data.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var inicio = DateTime.SpecifyKind(local.AddMinutes(-offsetMinutos), DateTimeKind.Utc);
        return (inicio, inicio.AddDays(1));
    }

    private static bool OffsetValido(int offsetMinutos) => offsetMinutos is >= -840 and <= 840;
    private static bool TryParseStatus(string? value, out StatusConsulta status) =>
        Enum.TryParse(value?.Trim(), true, out status) && Enum.IsDefined(typeof(StatusConsulta), status);

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

    private static object Snapshot(Consulta x) => new { x.Id, x.PacienteId, x.ProfissionalId, x.DataHoraUtc, x.Status, x.Motivo };
}
