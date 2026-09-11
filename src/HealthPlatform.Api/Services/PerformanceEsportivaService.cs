using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static partial class PerformanceEsportivaService
{
    public static async Task<PortalPerformanceResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        const int diasObservados = 180;
        var inicio = dia.AddDays(-(diasObservados - 1));
        var inicioUtc = inicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var itens = await db.ExecucoesItensTreino.AsNoTracking()
            .Where(x => x.ExecucaoTreino.PacienteId == pacienteId &&
                        x.ExecucaoTreino.Status == "Concluido" &&
                        x.ExecucaoTreino.DataHoraInicioUtc >= inicioUtc &&
                        x.ExecucaoTreino.DataHoraInicioUtc < fimUtc &&
                        x.Concluido)
            .Select(x => new
            {
                x.ItemTreino.ExercicioId,
                Exercicio = x.ItemTreino.Exercicio.Nome,
                GrupoMuscular = x.ItemTreino.Exercicio.GrupoMuscular,
                x.CargaRealizada,
                UnidadeCarga = x.UnidadeCarga ?? x.ItemTreino.UnidadeCarga,
                x.SeriesRealizadas,
                x.RepeticoesRealizadas,
                x.ExecucaoTreino.DataHoraInicioUtc
            })
            .ToListAsync(ct);

        var treinosPeriodo = await db.ExecucoesTreino.AsNoTracking().CountAsync(x =>
            x.PacienteId == pacienteId && x.Status == "Concluido" &&
            x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc, ct);

        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio && x.Data <= dia)
            .Select(x => new { x.Data, x.Score })
            .ToDictionaryAsync(x => x.Data, x => x.Score, ct);

        var inicio28 = dia.AddDays(-27).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var inicioAnterior28 = dia.AddDays(-55).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        decimal volume28 = 0m;
        decimal volumeAnterior28 = 0m;
        var possuiVolume28 = false;
        var possuiVolumeAnterior = false;

        foreach (var item in itens)
        {
            var volume = CalcularVolume(item.CargaRealizada, item.SeriesRealizadas, item.RepeticoesRealizadas);
            if (!volume.HasValue) continue;
            if (item.DataHoraInicioUtc >= inicio28)
            {
                volume28 += volume.Value;
                possuiVolume28 = true;
            }
            else if (item.DataHoraInicioUtc >= inicioAnterior28)
            {
                volumeAnterior28 += volume.Value;
                possuiVolumeAnterior = true;
            }
        }

        decimal? variacaoVolume = null;
        if (possuiVolume28 && possuiVolumeAnterior && volumeAnterior28 > 0m)
            variacaoVolume = Math.Round((volume28 - volumeAnterior28) / volumeAnterior28 * 100m, 1);

        var destaques = new List<PortalPerformanceExercicioResponse>();
        foreach (var grupo in itens.GroupBy(x => new { x.ExercicioId, x.Exercicio, x.GrupoMuscular }))
        {
            var ordenados = grupo.OrderBy(x => x.DataHoraInicioUtc).ToList();
            var comCarga = ordenados.Where(x => x.CargaRealizada.HasValue && x.CargaRealizada.Value > 0m).ToList();
            if (comCarga.Count == 0) continue;

            var melhor = comCarga.OrderByDescending(x => x.CargaRealizada).ThenByDescending(x => x.DataHoraInicioUtc).First();
            var ultima = comCarga.Last();
            var registrosAntesMelhor = comCarga.Where(x => x.DataHoraInicioUtc < melhor.DataHoraInicioUtc).ToList();
            var melhorAnterior = registrosAntesMelhor.Count == 0 ? (decimal?)null : registrosAntesMelhor.Max(x => x.CargaRealizada);
            var dataMelhor = DateOnly.FromDateTime(melhor.DataHoraInicioUtc);
            var novoPr = melhorAnterior.HasValue && melhor.CargaRealizada > melhorAnterior && dataMelhor >= dia.AddDays(-6);

            decimal? evolucao = null;
            var primeira = comCarga.First().CargaRealizada;
            if (primeira.HasValue && primeira.Value > 0m && melhor.CargaRealizada.HasValue)
                evolucao = Math.Round((melhor.CargaRealizada.Value - primeira.Value) / primeira.Value * 100m, 1);

            decimal? melhorVolume = null;
            foreach (var item in ordenados)
            {
                var volume = CalcularVolume(item.CargaRealizada, item.SeriesRealizadas, item.RepeticoesRealizadas);
                if (volume.HasValue && (!melhorVolume.HasValue || volume.Value > melhorVolume.Value)) melhorVolume = volume.Value;
            }

            prontidoes.TryGetValue(dataMelhor, out var scorePr);
            destaques.Add(new PortalPerformanceExercicioResponse(
                grupo.Key.ExercicioId, grupo.Key.Exercicio, grupo.Key.GrupoMuscular,
                melhor.CargaRealizada, melhor.UnidadeCarga, melhor.DataHoraInicioUtc,
                ultima.CargaRealizada, evolucao, melhorVolume, novoPr,
                prontidoes.ContainsKey(dataMelhor) ? scorePr : null, comCarga.Count));
        }

        var exerciciosAcompanhados = destaques.Count;
        var prsRecentes = destaques.Count(x => x.NovoPrRecente);
        destaques = destaques
            .OrderByDescending(x => x.NovoPrRecente)
            .ThenByDescending(x => x.DataMelhorCarga)
            .ThenBy(x => x.Exercicio)
            .Take(8)
            .ToList();
        string tendencia;
        string mensagem;
        if (treinosPeriodo < 2 || destaques.Count == 0)
        {
            tendencia = "DadosInsuficientes";
            mensagem = "Registre mais execuções para formar uma linha de base de performance.";
        }
        else if (!variacaoVolume.HasValue)
        {
            tendencia = prsRecentes > 0 ? "NovasMarcas" : "LinhaDeBase";
            mensagem = prsRecentes > 0
                ? $"{prsRecentes} nova(s) melhor(es) marca(s) recente(s). Interprete junto da recuperação e do ciclo."
                : "A linha de base está sendo construída; volume e melhores cargas já podem ser acompanhados.";
        }
        else if (variacaoVolume.Value >= 10m)
        {
            tendencia = "VolumeMaior";
            mensagem = "O volume estimado recente está maior que nas 4 semanas anteriores; avalie em conjunto com prontidão e objetivo do ciclo.";
        }
        else if (variacaoVolume.Value <= -15m)
        {
            tendencia = "VolumeMenor";
            mensagem = "O volume estimado recente está menor; isso pode refletir recuperação, deload ou mudança planejada do ciclo.";
        }
        else
        {
            tendencia = prsRecentes > 0 ? "NovasMarcas" : "Estavel";
            mensagem = prsRecentes > 0
                ? "O volume está relativamente estável, com novas melhores marcas recentes."
                : "Volume e melhores cargas estão relativamente estáveis no período comparado.";
        }

        return new PortalPerformanceResponse(
            diasObservados, treinosPeriodo, exerciciosAcompanhados, prsRecentes,
            possuiVolume28 ? Math.Round(volume28, 1) : null,
            possuiVolumeAnterior ? Math.Round(volumeAnterior28, 1) : null,
            variacaoVolume, tendencia, mensagem, destaques);
    }

    private static decimal? CalcularVolume(decimal? carga, int? series, string? repeticoes)
    {
        if (!carga.HasValue || carga.Value <= 0m) return null;
        var reps = EstimarRepeticoes(series, repeticoes);
        if (!reps.HasValue || reps.Value <= 0) return null;
        return Math.Round(carga.Value * reps.Value, 1);
    }

    private static int? EstimarRepeticoes(int? series, string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var numeros = RepetitionNumberRegex().Matches(texto).Select(x => int.Parse(x.Value)).ToList();
        if (numeros.Count == 0) return null;

        if (texto.Contains(',') || texto.Contains(';') || texto.Contains('/'))
            return numeros.Sum();

        var repeticoesPorSerie = numeros[0];
        return repeticoesPorSerie * Math.Max(1, series ?? 1);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex RepetitionNumberRegex();
}
