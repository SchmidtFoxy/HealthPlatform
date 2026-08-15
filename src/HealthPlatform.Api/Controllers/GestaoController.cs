using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record GestaoSerieItemResponse(string Rotulo, int Valor);

public sealed record GestaoPacienteAtencaoResponse(
    Guid PacienteId,
    string Nome,
    int PendenciasAbertas,
    int InsightsEstimados,
    bool SemRetornoFuturo,
    DateTime? UltimaConsultaUtc,
    DateTime? ProximaConsultaUtc);

public sealed record GestaoResumoResponse(
    int Dias,
    DateTime InicioUtc,
    DateTime FimUtc,
    int PacientesAtivos,
    int PacientesNovos,
    int ConsultasTotal,
    int ConsultasRealizadas,
    int ConsultasAgendadas,
    int ConsultasCanceladas,
    int Faltas,
    decimal TaxaComparecimentoPct,
    int FollowUpsRealizados,
    int FollowUpsVencidos,
    int PendenciasAbertas,
    int PendenciasResolvidasPeriodo,
    int TreinosRegistrados,
    int RegistrosDiario,
    int RegistrosMetas,
    IReadOnlyCollection<GestaoSerieItemResponse> ConsultasPorStatus,
    IReadOnlyCollection<GestaoSerieItemResponse> AtividadePorSemana,
    IReadOnlyCollection<GestaoPacienteAtencaoResponse> PacientesAtencao);

[ApiController]
[Authorize]
[Route("api/gestao")]
public sealed class GestaoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<GestaoResumoResponse>> Resumo(
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);
        var fim = DateTime.UtcNow;
        var inicio = fim.AddDays(-dias);
        var org = currentUser.OrganizationId;

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .Select(x => new { x.Id, x.Nome, x.CreatedAtUtc })
            .ToListAsync(ct);

        var ids = pacientes.Select(x => x.Id).ToArray();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x =>
                ids.Contains(x.PacienteId) &&
                x.DataHoraUtc >= inicio &&
                x.DataHoraUtc <= fim)
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.Status })
            .ToListAsync(ct);

        var todasConsultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.Status })
            .ToListAsync(ct);

        var realizados = consultas.Count(x => x.Status == StatusConsulta.Realizada);
        var faltas = consultas.Count(x => x.Status == StatusConsulta.Faltou);
        var canceladas = consultas.Count(x => x.Status == StatusConsulta.Cancelada);
        var agendadas = consultas.Count(x => x.Status == StatusConsulta.Agendada);
        var denominadorComparecimento = realizados + faltas;
        var taxa = denominadorComparecimento == 0
            ? 0m
            : Math.Round((decimal)realizados / denominadorComparecimento * 100m, 1);

        var followups = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                x.DataHoraUtc >= inicio &&
                x.DataHoraUtc <= fim)
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.ProximoContatoUtc })
            .ToListAsync(ct);

        var ultimoFollowPorPaciente = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.ProximoContatoUtc })
            .ToListAsync(ct);

        var followVencidos = ultimoFollowPorPaciente
            .GroupBy(x => x.PacienteId)
            .Select(g => g.OrderByDescending(x => x.DataHoraUtc).First())
            .Count(x => x.ProximoContatoUtc.HasValue && x.ProximoContatoUtc.Value < fim);

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new
            {
                x.PacienteId,
                x.Status,
                x.Severidade,
                x.ResolvidaEmUtc,
                x.AdiadaAteUtc
            })
            .ToListAsync(ct);

        var pendAbertas = pendencias.Count(x =>
            x.Status != "Resolvida" &&
            (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= fim));

        var pendResolvidas = pendencias.Count(x =>
            x.Status == "Resolvida" &&
            x.ResolvidaEmUtc.HasValue &&
            x.ResolvidaEmUtc.Value >= inicio &&
            x.ResolvidaEmUtc.Value <= fim);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .CountAsync(x =>
                ids.Contains(x.PacienteId) &&
                x.DataHoraInicioUtc >= inicio &&
                x.DataHoraInicioUtc <= fim, ct);

        var diario = await db.RegistrosDiarioPaciente.AsNoTracking()
            .CountAsync(x =>
                ids.Contains(x.PacienteId) &&
                x.DataHoraUtc >= inicio &&
                x.DataHoraUtc <= fim, ct);

        var inicioDate = DateOnly.FromDateTime(inicio);
        var fimDate = DateOnly.FromDateTime(fim);
        var metas = await db.RegistrosMetas.AsNoTracking()
            .CountAsync(x =>
                ids.Contains(x.MetaPaciente.PacienteId) &&
                x.Data >= inicioDate &&
                x.Data <= fimDate, ct);

        var statusSerie = new List<GestaoSerieItemResponse>
        {
            new("Realizadas", realizados),
            new("Agendadas", agendadas),
            new("Faltas", faltas),
            new("Canceladas", canceladas)
        };

        var semanas = new List<GestaoSerieItemResponse>();
        var cursor = inicio.Date;
        var index = 1;
        while (cursor <= fim.Date)
        {
            var ate = cursor.AddDays(7);
            var valor =
                consultas.Count(x => x.DataHoraUtc >= cursor && x.DataHoraUtc < ate) +
                followups.Count(x => x.DataHoraUtc >= cursor && x.DataHoraUtc < ate);
            semanas.Add(new GestaoSerieItemResponse($"S{index}", valor));
            index++;
            cursor = ate;
        }

        var resultadosRecentes = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x => ids.Contains(x.ExameLaboratorial.PacienteId))
            .Select(x => new
            {
                x.ExameLaboratorial.PacienteId,
                x.ExameLaboratorial.DataColetaUtc,
                x.ValorNumerico,
                x.ReferenciaMinima,
                x.ReferenciaMaxima
            })
            .ToListAsync(ct);

        var atencao = new List<GestaoPacienteAtencaoResponse>();

        foreach (var p in pacientes)
        {
            var cons = todasConsultas.Where(x => x.PacienteId == p.Id).ToList();
            var ultima = cons
                .Where(x => x.Status == StatusConsulta.Realizada)
                .OrderByDescending(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var proxima = cons
                .Where(x =>
                    x.DataHoraUtc > fim &&
                    x.Status != StatusConsulta.Cancelada &&
                    x.Status != StatusConsulta.Faltou &&
                    x.Status != StatusConsulta.Realizada)
                .OrderBy(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var pends = pendencias.Count(x =>
                x.PacienteId == p.Id &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= fim));

            var labs = resultadosRecentes.Where(x => x.PacienteId == p.Id).ToList();
            var ultimaDataLab = labs
                .OrderByDescending(x => x.DataColetaUtc)
                .Select(x => (DateTime?)x.DataColetaUtc)
                .FirstOrDefault();

            var alterados = ultimaDataLab.HasValue
                ? labs.Count(x =>
                    x.DataColetaUtc == ultimaDataLab.Value &&
                    x.ValorNumerico.HasValue &&
                    ((x.ReferenciaMinima.HasValue && x.ValorNumerico.Value < x.ReferenciaMinima.Value) ||
                     (x.ReferenciaMaxima.HasValue && x.ValorNumerico.Value > x.ReferenciaMaxima.Value)))
                : 0;

            var semRetorno = ultima.HasValue && !proxima.HasValue;
            if (pends == 0 && alterados == 0 && !semRetorno)
                continue;

            atencao.Add(new GestaoPacienteAtencaoResponse(
                p.Id,
                p.Nome,
                pends,
                alterados,
                semRetorno,
                ultima,
                proxima));
        }

        var top = atencao
            .OrderByDescending(x => x.PendenciasAbertas)
            .ThenByDescending(x => x.InsightsEstimados)
            .ThenByDescending(x => x.SemRetornoFuturo)
            .ThenBy(x => x.Nome)
            .Take(8)
            .ToList();

        return Ok(new GestaoResumoResponse(
            dias,
            inicio,
            fim,
            pacientes.Count,
            pacientes.Count(x => x.CreatedAtUtc >= inicio && x.CreatedAtUtc <= fim),
            consultas.Count,
            realizados,
            agendadas,
            canceladas,
            faltas,
            taxa,
            followups.Count,
            followVencidos,
            pendAbertas,
            pendResolvidas,
            treinos,
            diario,
            metas,
            statusSerie,
            semanas,
            top));
    }
}
