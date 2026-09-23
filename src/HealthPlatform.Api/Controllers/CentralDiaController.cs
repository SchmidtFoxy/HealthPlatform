using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record CentralDiaConsultaResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime DataHoraUtc,
    string Status,
    string? Motivo);

public sealed record CentralDiaFollowUpResponse(
    Guid PacienteId,
    string PacienteNome,
    DateTime ProximoContatoUtc,
    int DiasAtraso,
    string Faixa,
    string UltimoResultado,
    string UltimoCanal);

public sealed record CentralDiaPendenciaResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    string Titulo,
    string Severidade,
    DateTime? VencimentoUtc,
    string Status);

public sealed record CentralDiaPacienteResponse(
    Guid PacienteId,
    string PacienteNome,
    int PendenciasAbertas,
    bool SemRetornoFuturo,
    DateTime? UltimaConsultaUtc);

public sealed record CentralDiaSolicitacaoResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    string Tipo,
    string Titulo,
    DateTime? DataLimiteUtc,
    string Status,
    bool Vencida);

public sealed record CentralDiaAtencaoResponse(
    Guid PacienteId,
    string PacienteNome,
    string Nivel,
    int Score,
    int Eventos7Dias,
    int Recorrencias,
    int PendenciasAlta,
    bool FollowUpVencido,
    string Resumo,
    IReadOnlyCollection<string> Sinais);

public sealed record CentralDiaResponse(
    DateTime GeradoEmUtc,
    int ConsultasHoje,
    int FollowUpsVencidos,
    int FollowUpsHoje,
    int PendenciasPrioritarias,
    int SolicitacoesParaRevisao,
    int SolicitacoesVencidas,
    int PacientesRevisao,
    int PacientesEmAtencao,
    int EventosAdesao7Dias,
    IReadOnlyCollection<CentralDiaAtencaoResponse> Atencoes,
    IReadOnlyCollection<CentralDiaConsultaResponse> Consultas,
    IReadOnlyCollection<CentralDiaFollowUpResponse> FollowUps,
    IReadOnlyCollection<CentralDiaPendenciaResponse> Pendencias,
    IReadOnlyCollection<CentralDiaSolicitacaoResponse> Solicitacoes,
    IReadOnlyCollection<CentralDiaPacienteResponse> Pacientes);

[ApiController]
[Authorize]
[Route("api/central-dia")]
public sealed class CentralDiaController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CentralDiaResponse>> Get(
        [FromQuery] int offsetMinutos = 0,
        CancellationToken ct = default)
    {
        offsetMinutos = Math.Clamp(offsetMinutos, -840, 840);
        var agoraUtc = DateTime.UtcNow;
        var agoraLocal = agoraUtc.AddMinutes(offsetMinutos);
        var inicioLocal = agoraLocal.Date;
        var fimLocal = inicioLocal.AddDays(1);
        var inicioUtc = inicioLocal.AddMinutes(-offsetMinutos);
        var fimUtc = fimLocal.AddMinutes(-offsetMinutos);
        var org = currentUser.OrganizationId;

        var profissional = await db.Profissionais.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == org &&
                x.Ativo, ct);

        if (profissional is null)
            return Forbid();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.Paciente.OrganizacaoId == org &&
                x.ProfissionalId == profissional.Id &&
                x.DataHoraUtc >= inicioUtc &&
                x.DataHoraUtc < fimUtc &&
                x.Status != StatusConsulta.Cancelada)
            .OrderBy(x => x.DataHoraUtc)
            .Select(x => new CentralDiaConsultaResponse(
                x.Id,
                x.PacienteId,
                x.Paciente.Nome,
                x.DataHoraUtc,
                x.Status.ToString(),
                x.Motivo))
            .ToListAsync(ct);

        var interacoes = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                x.ProfissionalId == profissional.Id)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                PacienteNome = x.Paciente.Nome,
                x.DataHoraUtc,
                x.Canal,
                x.Resultado,
                x.ProximoContatoUtc
            })
            .ToListAsync(ct);

        var hoje = DateOnly.FromDateTime(agoraLocal);
        var followups = interacoes
            .GroupBy(x => new { x.PacienteId, x.PacienteNome })
            .Select(g => g.OrderByDescending(x => x.DataHoraUtc).First())
            .Where(x => x.ProximoContatoUtc.HasValue)
            .Select(x =>
            {
                var local = x.ProximoContatoUtc!.Value.AddMinutes(offsetMinutos);
                var data = DateOnly.FromDateTime(local);
                var atraso = data < hoje ? hoje.DayNumber - data.DayNumber : 0;
                var faixa = data < hoje ? "Vencido" : data == hoje ? "Hoje" : "Futuro";

                return new CentralDiaFollowUpResponse(
                    x.PacienteId,
                    x.PacienteNome,
                    x.ProximoContatoUtc.Value,
                    atraso,
                    faixa,
                    x.Resultado,
                    x.Canal);
            })
            .Where(x => x.Faixa != "Futuro")
            .OrderByDescending(x => x.DiasAtraso)
            .ThenBy(x => x.ProximoContatoUtc)
            .Take(20)
            .ToList();

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= agoraUtc) &&
                (x.Severidade == "Alta" ||
                 !x.VencimentoUtc.HasValue ||
                 x.VencimentoUtc.Value <= fimUtc))
            .OrderByDescending(x => x.Severidade == "Alta")
            .ThenBy(x => x.VencimentoUtc)
            .Take(20)
            .Select(x => new CentralDiaPendenciaResponse(
                x.Id,
                x.PacienteId,
                x.Paciente.Nome,
                x.Titulo,
                x.Severidade,
                x.VencimentoUtc,
                x.Status))
            .ToListAsync(ct);

        var solicitacoes = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                x.ProfissionalId == profissional.Id &&
                (x.Status == "Enviada" ||
                 (x.Status == "Pendente" && x.DataLimiteUtc.HasValue && x.DataLimiteUtc < agoraUtc)))
            .OrderBy(x => x.Status == "Enviada" ? 0 : 1)
            .ThenBy(x => x.DataLimiteUtc ?? DateTime.MaxValue)
            .Take(20)
            .Select(x => new CentralDiaSolicitacaoResponse(
                x.Id,
                x.PacienteId,
                x.Paciente.Nome,
                x.Tipo,
                x.Titulo,
                x.DataLimiteUtc,
                x.Status,
                x.Status == "Pendente" && x.DataLimiteUtc.HasValue && x.DataLimiteUtc < agoraUtc))
            .ToListAsync(ct);

        var solicitacoesParaRevisao = solicitacoes.Count(x => x.Status == "Enviada");
        var solicitacoesVencidas = solicitacoes.Count(x => x.Vencida);

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .Select(x => new { x.Id, x.Nome })
            .ToListAsync(ct);

        var pacienteIds = pacientes.Select(x => x.Id).ToArray();

        var consultasHistoricas = await db.Consultas.AsNoTracking()
            .Where(x => pacienteIds.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.Status })
            .ToListAsync(ct);

        var pendenciasAbertas = await db.PendenciasClinicas.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= agoraUtc))
            .Select(x => new { x.PacienteId, x.Severidade })
            .ToListAsync(ct);

        var desdeDesvios = agoraUtc.AddDays(-14);
        var registrosDesvio = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => pacienteIds.Contains(x.PacienteId) &&
                        x.Tipo == EventoDesvioAdesaoService.TipoRegistro &&
                        x.DataHoraUtc >= desdeDesvios)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(1000)
            .ToListAsync(ct);

        var desvios = registrosDesvio
            .Select(x => new { x.PacienteId, Evento = EventoDesvioAdesaoService.Ler(x) })
            .Where(x => x.Evento is not null)
            .Select(x => new { x.PacienteId, Evento = x.Evento! })
            .ToList();

        var revisao = new List<CentralDiaPacienteResponse>();

        foreach (var p in pacientes)
        {
            var cons = consultasHistoricas.Where(x => x.PacienteId == p.Id).ToList();

            var ultima = cons
                .Where(x => x.Status == StatusConsulta.Realizada)
                .OrderByDescending(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var proxima = cons.Any(x =>
                x.DataHoraUtc > agoraUtc &&
                x.Status != StatusConsulta.Cancelada &&
                x.Status != StatusConsulta.Faltou &&
                x.Status != StatusConsulta.Realizada);

            var abertas = pendenciasAbertas.Count(x => x.PacienteId == p.Id);

            if (abertas == 0 && (proxima || !ultima.HasValue))
                continue;

            revisao.Add(new CentralDiaPacienteResponse(
                p.Id,
                p.Nome,
                abertas,
                ultima.HasValue && !proxima,
                ultima));
        }

        var revisaoOrdenada = revisao
            .OrderByDescending(x => x.PendenciasAbertas)
            .ThenByDescending(x => x.SemRetornoFuturo)
            .ThenBy(x => x.UltimaConsultaUtc)
            .Take(10)
            .ToList();

        var seteDias = agoraUtc.AddDays(-7);
        var atencoes = new List<CentralDiaAtencaoResponse>();

        foreach (var p in pacientes)
        {
            var eventosPaciente = desvios
                .Where(x => x.PacienteId == p.Id)
                .Select(x => x.Evento)
                .ToList();
            var eventos7 = eventosPaciente.Count(x => x.DataHoraUtc >= seteDias);
            var recorrencias = eventosPaciente
                .GroupBy(x => new { x.Categoria, x.Tipo })
                .Where(g => g.Count() >= 2)
                .OrderByDescending(g => g.Max(x => x.Prioridade))
                .ThenByDescending(g => g.Count())
                .ToList();
            var pendenciasAltaPaciente = pendenciasAbertas.Count(x => x.PacienteId == p.Id && x.Severidade == "Alta");
            var followUpVencido = followups.Any(x => x.PacienteId == p.Id && x.Faixa == "Vencido");
            var maiorPrioridade = eventosPaciente.Count == 0 ? 0 : eventosPaciente.Max(x => x.Prioridade);

            var score = maiorPrioridade * 15;
            score += Math.Min(eventos7, 5) * 4;
            score += Math.Min(recorrencias.Count, 3) * 15;
            score += Math.Min(pendenciasAltaPaciente, 2) * 20;
            if (followUpVencido) score += 20;

            // A Central de Atenção só promove sinais úteis: recorrência, prioridade real ou pendência operacional.
            if (score < 20 || (maiorPrioridade < 2 && recorrencias.Count == 0 && pendenciasAltaPaciente == 0 && !followUpVencido))
                continue;

            var nivel = maiorPrioridade >= 3 || score >= 60 ? "Prioridade"
                : score >= 30 ? "Atencao"
                : "Observacao";

            var sinais = new List<string>();
            foreach (var grupo in recorrencias.Take(3))
                sinais.Add($"{grupo.Key.Categoria}: {grupo.Key.Tipo} recorrente ({grupo.Count()}x)");
            if (eventos7 > 0) sinais.Add($"{eventos7} evento(s) de adesao em 7 dias");
            if (pendenciasAltaPaciente > 0) sinais.Add($"{pendenciasAltaPaciente} pendencia(s) de alta prioridade");
            if (followUpVencido) sinais.Add("follow-up vencido");

            var resumo = recorrencias.Count > 0
                ? "Padrao recorrente detectado; revisar contexto antes de ajustar a conduta."
                : maiorPrioridade >= 3
                    ? "Evento recente de maior prioridade pede revisao profissional."
                    : pendenciasAltaPaciente > 0 || followUpVencido
                        ? "Ha pendencia operacional relevante para acompanhamento."
                        : "Sinais recentes merecem observacao profissional.";

            atencoes.Add(new CentralDiaAtencaoResponse(
                p.Id,
                p.Nome,
                nivel,
                score,
                eventos7,
                recorrencias.Count,
                pendenciasAltaPaciente,
                followUpVencido,
                resumo,
                sinais));
        }

        var atencoesOrdenadas = atencoes
            .OrderByDescending(x => x.Nivel == "Prioridade")
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.PacienteNome)
            .Take(12)
            .ToList();
        var eventosAdesao7Dias = desvios.Count(x => x.Evento.DataHoraUtc >= seteDias);

        return Ok(new CentralDiaResponse(
            agoraUtc,
            consultas.Count,
            followups.Count(x => x.Faixa == "Vencido"),
            followups.Count(x => x.Faixa == "Hoje"),
            pendencias.Count,
            solicitacoesParaRevisao,
            solicitacoesVencidas,
            revisaoOrdenada.Count,
            atencoesOrdenadas.Count,
            eventosAdesao7Dias,
            atencoesOrdenadas,
            consultas,
            followups,
            pendencias,
            solicitacoes,
            revisaoOrdenada));
    }
}
