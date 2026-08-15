using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record CarteiraPacienteResponse(
    Guid PacienteId,
    string Nome,
    string? Email,
    string? Telefone,
    DateOnly? DataNascimento,
    string? Sexo,
    DateTime? UltimaConsultaUtc,
    DateTime? ProximaConsultaUtc,
    DateTime? UltimaAvaliacaoUtc,
    decimal? PesoAtualKg,
    DateTime? UltimoExameUtc,
    int Insights,
    int InsightsAlta,
    int PendenciasAbertas,
    int PendenciasAlta,
    int TreinosUltimos30Dias,
    int RegistrosDiarioUltimos14Dias,
    int RegistrosMetaUltimos14Dias,
    DateTime? UltimoContatoUtc,
    DateTime? ProximoContatoUtc,
    int ContatosUltimos30Dias,
    string Prioridade,
    int Score,
    string MotivoPrioridade);

public sealed record CarteiraResumoResponse(
    int TotalPacientes,
    int Alta,
    int Media,
    int Baixa,
    int SemSinais,
    int SemRetornoFuturo,
    int ComPendencias,
    int ComInsights,
    IReadOnlyCollection<CarteiraPacienteResponse> Pacientes);

[ApiController]
[Authorize]
[Route("api/carteira")]
public sealed class CarteiraController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CarteiraResumoResponse>> Get(
        [FromQuery] string? busca = null,
        [FromQuery] string? prioridade = null,
        [FromQuery] string? ordenar = "score",
        CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var inicio14 = agora.AddDays(-14);
        var inicio30 = agora.AddDays(-30);

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Email,
                x.Telefone,
                x.DataNascimento,
                x.Sexo
            })
            .ToListAsync(ct);

        var ids = pacientes.Select(x => x.Id).ToArray();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.Status })
            .ToListAsync(ct);

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataUtc, x.PesoKg })
            .ToListAsync(ct);

        var exames = await db.ExamesLaboratoriais.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataColetaUtc })
            .ToListAsync(ct);

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x =>
                ids.Contains(x.PacienteId) &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= agora))
            .Select(x => new { x.PacienteId, x.Severidade })
            .ToListAsync(ct);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId) && x.DataHoraInicioUtc >= inicio30)
            .Select(x => x.PacienteId)
            .ToListAsync(ct);

        var diario = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId) && x.DataHoraUtc >= inicio14)
            .Select(x => x.PacienteId)
            .ToListAsync(ct);

        var interacoes = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new
            {
                x.PacienteId,
                x.DataHoraUtc,
                x.ProximoContatoUtc
            })
            .ToListAsync(ct);

        var dataInicioMeta = DateOnly.FromDateTime(inicio14);
        var metas = await db.RegistrosMetas.AsNoTracking()
            .Where(x => x.Data >= dataInicioMeta &&
                        ids.Contains(x.MetaPaciente.PacienteId))
            .Select(x => x.MetaPaciente.PacienteId)
            .ToListAsync(ct);

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

        var itens = new List<CarteiraPacienteResponse>();

        foreach (var p in pacientes)
        {
            var cons = consultas.Where(x => x.PacienteId == p.Id).ToList();

            var ultimaConsulta = cons
                .Where(x => x.Status == StatusConsulta.Realizada)
                .OrderByDescending(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var proximaConsulta = cons
                .Where(x =>
                    x.DataHoraUtc >= agora &&
                    x.Status != StatusConsulta.Cancelada &&
                    x.Status != StatusConsulta.Faltou &&
                    x.Status != StatusConsulta.Realizada)
                .OrderBy(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var avs = avaliacoes.Where(x => x.PacienteId == p.Id)
                .OrderByDescending(x => x.DataUtc)
                .ToList();

            var ultimaAvaliacao = avs.FirstOrDefault();

            var ultimoExame = exames.Where(x => x.PacienteId == p.Id)
                .OrderByDescending(x => x.DataColetaUtc)
                .Select(x => (DateTime?)x.DataColetaUtc)
                .FirstOrDefault();

            var pends = pendencias.Where(x => x.PacienteId == p.Id).ToList();
            var pendAlta = pends.Count(x => x.Severidade == "Alta");

            var lab = resultadosRecentes.Where(x => x.PacienteId == p.Id).ToList();
            var ultimaDataLab = lab.OrderByDescending(x => x.DataColetaUtc)
                .Select(x => (DateTime?)x.DataColetaUtc)
                .FirstOrDefault();

            var insightApprox = 0;
            var insightAlta = 0;

            if (ultimaDataLab.HasValue)
            {
                var alterados = lab.Count(x =>
                    x.DataColetaUtc == ultimaDataLab.Value &&
                    x.ValorNumerico.HasValue &&
                    ((x.ReferenciaMinima.HasValue && x.ValorNumerico.Value < x.ReferenciaMinima.Value) ||
                     (x.ReferenciaMaxima.HasValue && x.ValorNumerico.Value > x.ReferenciaMaxima.Value)));

                insightApprox += alterados;
                insightAlta += alterados;
            }

            if (avs.Count >= 2 &&
                avs[0].PesoKg.HasValue &&
                avs[1].PesoKg.HasValue &&
                avs[1].PesoKg.Value > 0)
            {
                var pct = Math.Abs(
                    (avs[0].PesoKg.Value - avs[1].PesoKg.Value) /
                    avs[1].PesoKg.Value * 100m);

                if (pct >= 3m)
                {
                    insightApprox++;
                    if (pct >= 7m) insightAlta++;
                }
            }

            var semRetorno = ultimaConsulta.HasValue &&
                             ultimaConsulta.Value <= agora.AddDays(-60) &&
                             !proximaConsulta.HasValue;

            if (semRetorno) insightApprox++;

            var treino30 = treinos.Count(x => x == p.Id);
            var diario14 = diario.Count(x => x == p.Id);
            var meta14 = metas.Count(x => x == p.Id);

            var contatosPaciente = interacoes.Where(x => x.PacienteId == p.Id).ToList();
            var ultimoContato = contatosPaciente
                .OrderByDescending(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();
            var proximoContato = contatosPaciente
                .Where(x => x.ProximoContatoUtc.HasValue && x.ProximoContatoUtc.Value >= agora)
                .OrderBy(x => x.ProximoContatoUtc)
                .Select(x => x.ProximoContatoUtc)
                .FirstOrDefault();
            var contatos30 = contatosPaciente.Count(x => x.DataHoraUtc >= inicio30);

            var score = 0;
            score += pendAlta * 40;
            score += Math.Max(0, pends.Count - pendAlta) * 18;
            score += insightAlta * 30;
            score += Math.Max(0, insightApprox - insightAlta) * 12;
            if (semRetorno) score += 20;
            if (ultimaAvaliacao is not null && ultimaAvaliacao.DataUtc <= agora.AddDays(-90)) score += 8;
            if (ultimoExame.HasValue && ultimoExame.Value <= agora.AddDays(-120)) score += 6;

            var prioridadeFinal = score >= 60 ? "Alta"
                : score >= 25 ? "Media"
                : score > 0 ? "Baixa"
                : "SemSinais";

            var motivos = new List<string>();
            if (pendAlta > 0) motivos.Add($"{pendAlta} pendência(s) alta");
            else if (pends.Count > 0) motivos.Add($"{pends.Count} pendência(s)");
            if (insightAlta > 0) motivos.Add($"{insightAlta} sinal(is) alto(s)");
            else if (insightApprox > 0) motivos.Add($"{insightApprox} sinal(is)");
            if (semRetorno) motivos.Add("sem retorno futuro");
            if (motivos.Count == 0) motivos.Add("acompanhamento estável");

            itens.Add(new CarteiraPacienteResponse(
                p.Id,
                p.Nome,
                p.Email,
                p.Telefone,
                p.DataNascimento,
                p.Sexo,
                ultimaConsulta,
                proximaConsulta,
                ultimaAvaliacao?.DataUtc,
                ultimaAvaliacao?.PesoKg,
                ultimoExame,
                insightApprox,
                insightAlta,
                pends.Count,
                pendAlta,
                treino30,
                diario14,
                meta14,
                ultimoContato,
                proximoContato,
                contatos30,
                prioridadeFinal,
                score,
                string.Join(" • ", motivos)));
        }

        IEnumerable<CarteiraPacienteResponse> query = itens;

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(x =>
                x.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (x.Email?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(prioridade) &&
            !prioridade.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.Prioridade.Equals(prioridade.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        query = (ordenar ?? "score").Trim().ToLowerInvariant() switch
        {
            "nome" => query.OrderBy(x => x.Nome),
            "retorno" => query
                .OrderBy(x => x.ProximaConsultaUtc ?? DateTime.MaxValue)
                .ThenByDescending(x => x.Score),
            "consulta" => query
                .OrderByDescending(x => x.UltimaConsultaUtc ?? DateTime.MinValue),
            _ => query.OrderByDescending(x => x.Score).ThenBy(x => x.Nome)
        };

        var final = query.ToList();

        return Ok(new CarteiraResumoResponse(
            itens.Count,
            itens.Count(x => x.Prioridade == "Alta"),
            itens.Count(x => x.Prioridade == "Media"),
            itens.Count(x => x.Prioridade == "Baixa"),
            itens.Count(x => x.Prioridade == "SemSinais"),
            itens.Count(x => x.UltimaConsultaUtc.HasValue && !x.ProximaConsultaUtc.HasValue),
            itens.Count(x => x.PendenciasAbertas > 0),
            itens.Count(x => x.Insights > 0),
            final));
    }
}
