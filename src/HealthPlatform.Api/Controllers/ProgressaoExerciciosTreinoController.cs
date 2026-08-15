using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
public sealed class ProgressaoExerciciosTreinoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    private sealed record RegistroCarga(
        Guid ExercicioId,
        string Exercicio,
        string GrupoMuscular,
        DateTime DataHoraInicioUtc,
        decimal Carga,
        string Unidade,
        int? SeriesRealizadas,
        string? RepeticoesRealizadas,
        int? EsforcoPercebido);

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/treinos/progressao-exercicios")]
    public async Task<IActionResult> Profissional(
        Guid pacienteId,
        [FromQuery] int dias = 180,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await Montar(pacienteId, dias, ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/treinos/progressao-exercicios")]
    public async Task<IActionResult> Paciente(
        [FromQuery] int dias = 180,
        CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await Montar(pacienteId.Value, dias, ct));
    }

    private async Task<object> Montar(
        Guid pacienteId,
        int dias,
        CancellationToken ct)
    {
        dias = Math.Clamp(dias, 14, 730);
        var desde = DateTime.UtcNow.AddDays(-dias);

        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Itens)
                .ThenInclude(x => x.ItemTreino)
                .ThenInclude(x => x.Exercicio)
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                x.DataHoraInicioUtc >= desde &&
                x.Status == "Concluido")
            .OrderBy(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var registros = execucoes
            .SelectMany(execucao => execucao.Itens
                .Where(item =>
                    item.Concluido &&
                    item.CargaRealizada.HasValue &&
                    item.CargaRealizada.Value >= 0m)
                .Select(item => new RegistroCarga(
                    item.ItemTreino.ExercicioId,
                    item.ItemTreino.Exercicio.Nome,
                    Grupo(item.ItemTreino.Exercicio.GrupoMuscular),
                    execucao.DataHoraInicioUtc,
                    item.CargaRealizada!.Value,
                    NormalizarUnidade(item.UnidadeCarga ?? item.ItemTreino.UnidadeCarga),
                    item.SeriesRealizadas,
                    Limpar(item.RepeticoesRealizadas),
                    item.EsforcoPercebido)))
            .OrderBy(x => x.DataHoraInicioUtc)
            .ToList();

        var series = registros
            .GroupBy(x => new
            {
                x.ExercicioId,
                x.Exercicio,
                x.GrupoMuscular,
                x.Unidade
            })
            .Select(grupo =>
            {
                var pontos = grupo.OrderBy(x => x.DataHoraInicioUtc).ToList();
                var primeiro = pontos.First();
                var ultimo = pontos.Last();
                var melhor = pontos
                    .OrderByDescending(x => x.Carga)
                    .ThenByDescending(x => x.DataHoraInicioUtc)
                    .First();

                var delta = Math.Round(ultimo.Carga - primeiro.Carga, 2);
                decimal? percentual = primeiro.Carga > 0m
                    ? Math.Round(delta * 100m / primeiro.Carga, 1)
                    : null;

                var novosRecordes = ContarNovosRecordes(pontos);
                var tendencia = Tendencia(pontos);

                return new
                {
                    grupo.Key.ExercicioId,
                    exercicio = grupo.Key.Exercicio,
                    grupoMuscular = grupo.Key.GrupoMuscular,
                    unidade = grupo.Key.Unidade,
                    registros = pontos.Count,
                    primeiraCarga = primeiro.Carga,
                    primeiraCargaUtc = primeiro.DataHoraInicioUtc,
                    ultimaCarga = ultimo.Carga,
                    ultimaCargaUtc = ultimo.DataHoraInicioUtc,
                    maiorCarga = melhor.Carga,
                    maiorCargaUtc = melhor.DataHoraInicioUtc,
                    deltaCarga = delta,
                    variacaoPercentual = percentual,
                    novosRecordes,
                    tendenciaCarga = tendencia,
                    ultimaExecucao = new
                    {
                        ultimo.SeriesRealizadas,
                        ultimo.RepeticoesRealizadas,
                        ultimo.EsforcoPercebido
                    },
                    pontos = pontos.Select(x => new
                    {
                        x.DataHoraInicioUtc,
                        cargaRealizada = x.Carga,
                        x.SeriesRealizadas,
                        x.RepeticoesRealizadas,
                        x.EsforcoPercebido
                    }).ToList()
                };
            })
            .OrderByDescending(x => x.registros)
            .ThenBy(x => x.exercicio)
            .ThenBy(x => x.unidade)
            .ToList();

        var comBase = series.Where(x => x.registros >= 2).ToList();
        var maiorEvolucao = comBase
            .Where(x => x.variacaoPercentual.HasValue)
            .OrderByDescending(x => x.variacaoPercentual)
            .ThenByDescending(x => x.registros)
            .FirstOrDefault();

        var maisRecordes = series
            .OrderByDescending(x => x.novosRecordes)
            .ThenByDescending(x => x.registros)
            .FirstOrDefault();

        return new
        {
            pacienteId,
            dias,
            desdeUtc = desde,
            resumo = new
            {
                execucoesPeriodo = execucoes.Count,
                exerciciosComCarga = registros.Select(x => x.ExercicioId).Distinct().Count(),
                seriesDeCarga = series.Count,
                exerciciosComBaseComparativa = comBase.Select(x => x.ExercicioId).Distinct().Count(),
                seriesAcimaDaBase = series.Count(x => x.tendenciaCarga == "AcimaDaBase"),
                novosRecordesPeriodo = series.Sum(x => x.novosRecordes),
                ultimaExecucaoUtc = execucoes.LastOrDefault()?.DataHoraInicioUtc
            },
            destaques = new
            {
                maiorEvolucao = maiorEvolucao is null ? null : new
                {
                    maiorEvolucao.ExercicioId,
                    maiorEvolucao.exercicio,
                    maiorEvolucao.grupoMuscular,
                    maiorEvolucao.unidade,
                    maiorEvolucao.variacaoPercentual,
                    maiorEvolucao.deltaCarga,
                    maiorEvolucao.registros
                },
                maisRecordes = maisRecordes is null || maisRecordes.novosRecordes == 0 ? null : new
                {
                    maisRecordes.ExercicioId,
                    maisRecordes.exercicio,
                    maisRecordes.grupoMuscular,
                    maisRecordes.unidade,
                    maisRecordes.novosRecordes,
                    maisRecordes.maiorCarga,
                    maisRecordes.maiorCargaUtc
                }
            },
            exercicios = series,
            observacao = "Cargas sao comparadas somente dentro do mesmo exercicio e da mesma unidade. Nao ha estimativa de 1RM nem calculo de tonelagem a partir de repeticoes textuais."
        };
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private static int ContarNovosRecordes(IReadOnlyList<RegistroCarga> pontos)
    {
        if (pontos.Count < 2)
            return 0;

        var maiorAnterior = pontos[0].Carga;
        var novos = 0;

        foreach (var ponto in pontos.Skip(1))
        {
            if (ponto.Carga > maiorAnterior)
            {
                novos++;
                maiorAnterior = ponto.Carga;
            }
        }

        return novos;
    }

    private static string Tendencia(IReadOnlyList<RegistroCarga> pontos)
    {
        if (pontos.Count < 2)
            return "SemBase";

        decimal baseCarga;
        decimal recente;

        if (pontos.Count >= 4)
        {
            var recentes = pontos.TakeLast(Math.Min(3, pontos.Count)).ToList();
            var anteriores = pontos
                .Take(Math.Max(1, pontos.Count - recentes.Count))
                .TakeLast(Math.Min(3, pontos.Count - recentes.Count))
                .ToList();

            baseCarga = anteriores.Count > 0
                ? Math.Round(anteriores.Average(x => x.Carga), 2)
                : pontos.First().Carga;
            recente = Math.Round(recentes.Average(x => x.Carga), 2);
        }
        else
        {
            baseCarga = pontos.First().Carga;
            recente = pontos.Last().Carga;
        }

        if (baseCarga == 0m)
            return recente > 0m ? "AcimaDaBase" : "Estavel";

        var variacao = (recente - baseCarga) * 100m / baseCarga;

        if (variacao > 2m)
            return "AcimaDaBase";
        if (variacao < -2m)
            return "AbaixoDaBase";

        return "Estavel";
    }

    private static string NormalizarUnidade(string? unidade)
    {
        if (string.IsNullOrWhiteSpace(unidade))
            return "sem unidade";

        var valor = unidade.Trim().ToLowerInvariant();

        return valor switch
        {
            "kg" or "kgs" or "quilo" or "quilos" => "kg",
            "lb" or "lbs" or "libra" or "libras" => "lb",
            _ => unidade.Trim()
        };
    }

    private static string Grupo(string? grupo) =>
        string.IsNullOrWhiteSpace(grupo) ? "Nao informado" : grupo.Trim();

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
