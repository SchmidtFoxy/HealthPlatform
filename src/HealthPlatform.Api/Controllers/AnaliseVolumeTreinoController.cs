using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AnaliseVolumeTreinoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    private sealed record VolumeGrupoTemporario(
        string GrupoMuscular,
        int SeriesPorCiclo,
        int SeriesSemanaisEstimadas,
        int ExerciciosDistintos);

    [HttpGet("api/pacientes/{pacienteId:guid}/treinos/analise-volume")]
    public async Task<IActionResult> Analisar(
        Guid pacienteId,
        [FromQuery] Guid? planoId = null,
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);

        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente nao encontrado." });

        PlanoTreino? plano;

        if (planoId.HasValue)
        {
            plano = await QueryPlano().FirstOrDefaultAsync(x =>
                x.Id == planoId.Value &&
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        }
        else
        {
            plano = await QueryPlano()
                .Where(x =>
                    x.PacienteId == pacienteId &&
                    x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                    x.Status == "Ativo")
                .OrderByDescending(x => x.DataInicio)
                .ThenByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            plano ??= await QueryPlano()
                .Where(x =>
                    x.PacienteId == pacienteId &&
                    x.Paciente.OrganizacaoId == currentUser.OrganizationId)
                .OrderByDescending(x => x.DataInicio)
                .ThenByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
        }

        if (plano is null)
            return NotFound(new { message = "Plano de treino nao encontrado para analise." });

        var sessoes = plano.Sessoes.OrderBy(x => x.Ordem).ToList();
        var itensPlano = sessoes.SelectMany(x => x.Itens).ToList();

        var volumePlanejado = itensPlano
            .GroupBy(x => Grupo(x.Exercicio.GrupoMuscular))
            .Select(g =>
            {
                var seriesCiclo = g.Sum(x => x.Series);
                var seriesSemana = g.Sum(x =>
                    x.Series * FrequenciaSemanal(x.SessaoTreino.DiasSemana).Frequencia);

                return new VolumeGrupoTemporario(
                    g.Key,
                    seriesCiclo,
                    seriesSemana,
                    g.Select(x => x.ExercicioId).Distinct().Count());
            })
            .ToList();

        var desde = DateTime.UtcNow.AddDays(-dias);
        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Itens)
                .ThenInclude(x => x.ItemTreino)
                .ThenInclude(x => x.Exercicio)
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                x.PlanoTreinoId == plano.Id &&
                x.DataHoraInicioUtc >= desde &&
                x.Status == "Concluido")
            .OrderBy(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var realizadas = execucoes
            .SelectMany(x => x.Itens)
            .Where(x => x.Concluido)
            .GroupBy(x => Grupo(x.ItemTreino.Exercicio.GrupoMuscular))
            .ToDictionary(
                x => x.Key,
                x => x.Sum(i => Math.Max(0, i.SeriesRealizadas ?? 0)));

        var grupos = volumePlanejado.Select(x => x.GrupoMuscular)
            .Concat(realizadas.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var totalSemanalPlanejado = volumePlanejado.Sum(x => x.SeriesSemanaisEstimadas);
        var totalRealizado = realizadas.Values.Sum();
        var semanasPeriodo = dias / 7m;
        var mediaRealizadaSemanal = semanasPeriodo > 0
            ? Math.Round(totalRealizado / semanasPeriodo, 1)
            : 0m;

        var porGrupo = grupos.Select(grupo =>
        {
            var planejado = volumePlanejado.FirstOrDefault(x =>
                string.Equals(x.GrupoMuscular, grupo, StringComparison.OrdinalIgnoreCase));

            var seriesSemanais = planejado?.SeriesSemanaisEstimadas ?? 0;
            var seriesRealizadas = realizadas.TryGetValue(grupo, out var valor) ? valor : 0;

            return new
            {
                grupoMuscular = grupo,
                seriesPorCiclo = planejado?.SeriesPorCiclo ?? 0,
                seriesSemanaisEstimadas = seriesSemanais,
                exerciciosDistintos = planejado?.ExerciciosDistintos ?? 0,
                percentualDoVolumeSemanal = totalSemanalPlanejado > 0
                    ? Math.Round(seriesSemanais * 100m / totalSemanalPlanejado, 1)
                    : 0m,
                seriesRealizadasPeriodo = seriesRealizadas,
                mediaSeriesRealizadasSemana = semanasPeriodo > 0
                    ? Math.Round(seriesRealizadas / semanasPeriodo, 1)
                    : 0m
            };
        })
        .OrderByDescending(x => x.seriesSemanaisEstimadas)
        .ThenByDescending(x => x.seriesRealizadasPeriodo)
        .ThenBy(x => x.grupoMuscular)
        .ToList();

        var porSessao = sessoes.Select(sessao =>
        {
            var frequencia = FrequenciaSemanal(sessao.DiasSemana);
            var seriesSessao = sessao.Itens.Sum(x => x.Series);

            return new
            {
                sessao.Id,
                sessao.Nome,
                sessao.DiasSemana,
                frequenciaSemanal = frequencia.Frequencia,
                frequenciaInferida = frequencia.Inferida,
                seriesPorSessao = seriesSessao,
                seriesSemanaisEstimadas = seriesSessao * frequencia.Frequencia,
                exercicios = sessao.Itens.Select(x => new
                {
                    x.Id,
                    x.ExercicioId,
                    exercicio = x.Exercicio.Nome,
                    grupoMuscular = Grupo(x.Exercicio.GrupoMuscular),
                    x.Series,
                    x.Repeticoes,
                    x.Carga,
                    x.UnidadeCarga
                }).ToList()
            };
        }).ToList();

        var maiorConcentracao = porGrupo.FirstOrDefault();

        return Ok(new
        {
            pacienteId,
            dias,
            desdeUtc = desde,
            plano = new
            {
                plano.Id,
                plano.Nome,
                plano.Versao,
                plano.Status,
                plano.DataInicio,
                plano.DataFim
            },
            resumo = new
            {
                sessoes = sessoes.Count,
                exerciciosDistintos = itensPlano.Select(x => x.ExercicioId).Distinct().Count(),
                gruposMusculares = porGrupo.Count,
                seriesPorCiclo = itensPlano.Sum(x => x.Series),
                seriesSemanaisEstimadas = totalSemanalPlanejado,
                execucoesPeriodo = execucoes.Count,
                seriesRealizadasPeriodo = totalRealizado,
                mediaSeriesRealizadasSemana = mediaRealizadaSemanal,
                maiorConcentracaoGrupo = maiorConcentracao?.grupoMuscular,
                maiorConcentracaoPercentual = maiorConcentracao?.percentualDoVolumeSemanal
            },
            porGrupo,
            porSessao,
            observacao = "Series semanais sao estimadas pela frequencia reconhecida em DiasSemana. Tonelagem nao e inferida quando a prescricao de repeticoes nao e numerica."
        });
    }

    private IQueryable<PlanoTreino> QueryPlano() =>
        db.PlanosTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Sessoes)
                .ThenInclude(x => x.Itens)
                .ThenInclude(x => x.Exercicio);

    private static string Grupo(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "Nao informado" : valor.Trim();

    private static (int Frequencia, bool Inferida) FrequenciaSemanal(string? diasSemana)
    {
        if (string.IsNullOrWhiteSpace(diasSemana))
            return (1, false);

        var texto = SemAcentos(diasSemana);
        var matches = Regex.Matches(
            texto,
            @"\b(seg|segunda|ter|terca|qua|quarta|qui|quinta|sex|sexta|sab|sabado|dom|domingo)\b",
            RegexOptions.IgnoreCase);

        var dias = matches
            .Select(x => NormalizarDia(x.Value))
            .Where(x => x is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return dias > 0 ? (dias, true) : (1, false);
    }

    private static string? NormalizarDia(string valor) =>
        valor.ToLowerInvariant() switch
        {
            "seg" or "segunda" => "seg",
            "ter" or "terca" => "ter",
            "qua" or "quarta" => "qua",
            "qui" or "quinta" => "qui",
            "sex" or "sexta" => "sex",
            "sab" or "sabado" => "sab",
            "dom" or "domingo" => "dom",
            _ => null
        };

    private static string SemAcentos(string valor)
    {
        var normalizado = valor.Normalize(NormalizationForm.FormD);
        var chars = normalizado
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars)
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }
}
