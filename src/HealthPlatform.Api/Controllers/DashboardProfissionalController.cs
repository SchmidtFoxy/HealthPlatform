using HealthPlatform.Api.Contracts.Dashboard;
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
[Route("api/profissional/dashboard")]
public class DashboardProfissionalController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardProfissionalResponse>> Get([FromQuery] DateOnly? data, [FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        if (offsetMinutos is < -840 or > 840) return BadRequest(new { message = "offsetMinutos deve estar entre -840 e 840." });
        var profissional = await db.Profissionais.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de abrir o dashboard." });

        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(offsetMinutos));
        var inicioLocal = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var inicioUtc = DateTime.SpecifyKind(inicioLocal.AddMinutes(-offsetMinutos), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);
        var agoraUtc = DateTime.UtcNow;
        var trintaDiasAtras = agoraUtc.AddDays(-30);
        var seteDiasAtras = agoraUtc.AddDays(-7);
        var noventaDiasAtras = agoraUtc.AddDays(-90);
        var vinteUmDiasAtras = agoraUtc.AddDays(-21);

        var consultasHojeEntity = await db.Consultas.AsNoTracking()
            .Include(x => x.Paciente)
            .Where(x => x.ProfissionalId == profissional.Id && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.DataHoraUtc >= inicioUtc && x.DataHoraUtc < fimUtc)
            .OrderBy(x => x.DataHoraUtc)
            .ToListAsync(ct);

        var proximasEntity = await db.Consultas.AsNoTracking()
            .Include(x => x.Paciente)
            .Where(x => x.ProfissionalId == profissional.Id && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.DataHoraUtc >= agoraUtc && x.Status != StatusConsulta.Cancelada && x.Status != StatusConsulta.Faltou)
            .OrderBy(x => x.DataHoraUtc)
            .Take(8)
            .ToListAsync(ct);

        var pacientesAtivos = await db.Pacientes.AsNoTracking().CountAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        var pacientesAtendidos30 = await db.Consultas.AsNoTracking()
            .Where(x => x.ProfissionalId == profissional.Id && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.Status == StatusConsulta.Realizada && x.DataHoraUtc >= trintaDiasAtras)
            .Select(x => x.PacienteId).Distinct().CountAsync(ct);

        var candidatosRetorno = await db.Consultas.AsNoTracking()
            .Where(x => x.ProfissionalId == profissional.Id && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.Paciente.Ativo && x.Status == StatusConsulta.Realizada && x.DataHoraUtc >= noventaDiasAtras && x.DataHoraUtc <= vinteUmDiasAtras)
            .GroupBy(x => new { x.PacienteId, x.Paciente.Nome })
            .Select(g => new { g.Key.PacienteId, g.Key.Nome, UltimaConsultaUtc = g.Max(x => x.DataHoraUtc) })
            .ToListAsync(ct);

        var idsCandidatos = candidatosRetorno.Select(x => x.PacienteId).ToList();
        var comConsultaFutura = await db.Consultas.AsNoTracking()
            .Where(x => idsCandidatos.Contains(x.PacienteId) && x.ProfissionalId == profissional.Id && x.DataHoraUtc > agoraUtc && x.Status != StatusConsulta.Cancelada && x.Status != StatusConsulta.Faltou)
            .Select(x => x.PacienteId).Distinct().ToListAsync(ct);
        var retornoSet = candidatosRetorno.Where(x => !comConsultaFutura.Contains(x.PacienteId)).ToDictionary(x => x.PacienteId, x => x.UltimaConsultaUtc);

        var ultimosRegistros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.Paciente.Ativo)
            .GroupBy(x => x.PacienteId)
            .Select(g => new { PacienteId = g.Key, Ultimo = g.Max(x => x.DataHoraUtc) })
            .ToDictionaryAsync(x => x.PacienteId, x => (DateTime?)x.Ultimo, ct);

        var atencaoBase = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                UltimaConsultaUtc = x.Consultas.Where(c => c.ProfissionalId == profissional.Id && c.Status == StatusConsulta.Realizada).Select(c => (DateTime?)c.DataHoraUtc).Max()
            })
            .ToListAsync(ct);

        var atencao = atencaoBase
            .Select(x =>
            {
                ultimosRegistros.TryGetValue(x.Id, out var ultimoRegistro);
                var diasSem = ultimoRegistro.HasValue ? Math.Max(0, (int)Math.Floor((agoraUtc - ultimoRegistro.Value).TotalDays)) : 999;
                var retorno = retornoSet.ContainsKey(x.Id);
                return new DashboardPacienteAtencaoResponse(x.Id, x.Nome, x.UltimaConsultaUtc, ultimoRegistro, diasSem, retorno);
            })
            .Where(x => x.RetornoPendente || !x.UltimoRegistroDiarioUtc.HasValue || x.UltimoRegistroDiarioUtc < seteDiasAtras)
            .OrderByDescending(x => x.RetornoPendente)
            .ThenByDescending(x => x.DiasSemRegistroDiario)
            .Take(8)
            .ToList();

        var recentes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(6)
            .Select(x => new DashboardPacienteRecenteResponse(
                x.Id, x.Nome, x.CreatedAtUtc,
                x.Consultas.Where(c => c.ProfissionalId == profissional.Id).Select(c => (DateTime?)c.DataHoraUtc).Max()))
            .ToListAsync(ct);

        return Ok(new DashboardProfissionalResponse(
            dia,
            offsetMinutos,
            profissional.Nome,
            pacientesAtivos,
            pacientesAtendidos30,
            consultasHojeEntity.Count,
            consultasHojeEntity.Count(x => x.Status == StatusConsulta.Confirmada),
            consultasHojeEntity.Count(x => x.Status == StatusConsulta.Realizada),
            consultasHojeEntity.Count(x => x.Status == StatusConsulta.Faltou),
            retornoSet.Count,
            consultasHojeEntity.Select(x => ToConsulta(x, offsetMinutos)).ToList(),
            proximasEntity.Select(x => ToConsulta(x, offsetMinutos)).ToList(),
            atencao,
            recentes));
    }

    private static DashboardConsultaResumoResponse ToConsulta(Consulta x, int offsetMinutos) => new(
        x.Id, x.PacienteId, x.Paciente.Nome, x.DataHoraUtc, DateTime.SpecifyKind(x.DataHoraUtc.AddMinutes(offsetMinutos), DateTimeKind.Unspecified), x.Status.ToString(), x.Motivo);
}
