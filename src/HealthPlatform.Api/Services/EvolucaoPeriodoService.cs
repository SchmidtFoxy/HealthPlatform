using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.Evolucao;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Evolucao do paciente num periodo, comparada com o periodo anterior de mesmo tamanho:
/// treino (sessoes, series, volume), recuperacao (prontidao e sono), dieta (adesao registrada),
/// corpo (avaliacoes) e fotos. A mesma resposta alimenta a tela do paciente e a ficha do profissional.
/// </summary>
public static partial class EvolucaoPeriodoService
{
    private sealed record Serie(DateOnly Dia, string Grupo, int Series, decimal VolumeKg);
    private sealed record Sessao(DateOnly Dia, int Minutos, int? Esforco);
    private sealed record Prontidao(DateOnly Dia, int Score, decimal SonoHoras);
    private sealed record Refeicao(DateOnly Dia, decimal Pontos);

    public static async Task<EvolucaoPeriodoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, int dias, bool paraPaciente, CancellationToken ct)
    {
        dias = Math.Clamp(dias, 14, 365);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioAtual = hoje.AddDays(-(dias - 1));
        var fimAnterior = inicioAtual.AddDays(-1);
        var inicioAnterior = fimAnterior.AddDays(-(dias - 1));
        var desdeUtc = Utc(inicioAnterior);
        var ateUtc = Utc(hoje.AddDays(1));

        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.Itens).ThenInclude(x => x.ItemTreino).ThenInclude(x => x.Exercicio)
            .Include(x => x.Extras).ThenInclude(x => x.Exercicio)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= desdeUtc && x.DataHoraInicioUtc < ateUtc)
            .ToListAsync(ct);

        var sessoes = execucoes
            .Select(x => new Sessao(DateOnly.FromDateTime(x.DataHoraInicioUtc), x.DuracaoMinutos ?? 0, x.EsforcoPercebido))
            .ToList();
        var series = execucoes.SelectMany(x =>
        {
            var dia = DateOnly.FromDateTime(x.DataHoraInicioUtc);
            var feitos = x.Itens.Where(i => i.Concluido).Select(i => new Serie(dia,
                Grupo(i.ItemTreino.Exercicio.GrupoMuscular),
                Math.Max(0, i.SeriesRealizadas ?? 0),
                Volume(i.CargaRealizada, i.UnidadeCarga, i.SeriesRealizadas, i.RepeticoesRealizadas)));
            var extras = x.Extras.Select(e => new Serie(dia,
                Grupo(e.Exercicio.GrupoMuscular),
                Math.Max(0, e.SeriesRealizadas ?? 0),
                Volume(e.CargaRealizada, e.UnidadeCarga, e.SeriesRealizadas, e.RepeticoesRealizadas)));
            return feitos.Concat(extras);
        }).ToList();

        var prontidoes = (await db.ProntidoesDiarias.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.Data >= inicioAnterior && x.Data <= hoje)
                .Select(x => new { x.Data, x.Score, x.SonoHoras, x.CreatedAtUtc })
                .ToListAsync(ct))
            .GroupBy(x => x.Data)
            .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
            .Select(x => new Prontidao(x.Data, x.Score, x.SonoHoras))
            .ToList();

        // Adesao: vale o ultimo registro de cada refeicao no dia (realizada 100, adaptada 80, nao realizada 0),
        // a mesma regra da adesao do dia na tela Hoje.
        var registrosAdesao = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "AdesaoRefeicao" &&
                        x.DataHoraUtc >= desdeUtc && x.DataHoraUtc < ateUtc)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new { x.DataHoraUtc, x.Descricao })
            .ToListAsync(ct);
        var refeicoes = new List<Refeicao>();
        var vistas = new HashSet<(DateOnly, Guid)>();
        foreach (var r in registrosAdesao)
        {
            if (AdesaoNutricionalService.LerStatus(r.Descricao) is not { } lido) continue;
            var dia = DateOnly.FromDateTime(r.DataHoraUtc);
            if (!vistas.Add((dia, lido.RefeicaoId))) continue;
            refeicoes.Add(new Refeicao(dia, lido.Status switch { "Realizada" => 100m, "Adaptada" => 80m, _ => 0m }));
        }

        var avaliacoes = (await db.Avaliacoes.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId)
                .OrderByDescending(x => x.DataUtc)
                .Take(60)
                .ToListAsync(ct))
            .OrderBy(x => x.DataUtc)
            .Select(x => new EvolucaoAvaliacaoResponse(
                x.Id,
                x.DataUtc,
                x.PesoKg,
                x.PesoKg.HasValue && x.AlturaM.HasValue && x.AlturaM.Value > 0m ? Math.Round(x.PesoKg.Value / (x.AlturaM.Value * x.AlturaM.Value), 1) : null,
                x.PercentualGordura,
                x.MassaMagraKg,
                x.MassaGordaKg,
                x.CinturaCm,
                x.AbdomenCm,
                x.QuadrilCm))
            .ToList();

        var fotos = (await db.RegistrosDiarioPaciente.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.Tipo.StartsWith("FotoEvolucao") && x.ImagemUrl != null)
                .OrderByDescending(x => x.DataHoraUtc)
                .Take(60)
                .Select(x => new { x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ImagemUrl })
                .ToListAsync(ct))
            .Select(x => new EvolucaoFotoResponse(
                x.Id,
                x.DataHoraUtc,
                Pose(x.Tipo),
                x.Tipo.StartsWith("FotoEvolucaoAnotada", StringComparison.Ordinal),
                x.Descricao,
                paraPaciente ? UrlDoPaciente(x.ImagemUrl!) : x.ImagemUrl!))
            .ToList();

        EvolucaoResumoPeriodoResponse Resumo(DateOnly inicio, DateOnly fim)
        {
            bool Dentro(DateOnly d) => d >= inicio && d <= fim;
            var s = sessoes.Where(x => Dentro(x.Dia)).ToList();
            var sr = series.Where(x => Dentro(x.Dia)).ToList();
            var p = prontidoes.Where(x => Dentro(x.Dia)).ToList();
            var r = refeicoes.Where(x => Dentro(x.Dia)).ToList();
            var fimUtc = Utc(fim.AddDays(1));
            decimal? Ultima(Func<EvolucaoAvaliacaoResponse, decimal?> campo) =>
                avaliacoes.LastOrDefault(a => a.DataUtc < fimUtc && campo(a).HasValue) is { } a ? campo(a) : null;
            return new EvolucaoResumoPeriodoResponse(
                inicio, fim,
                s.Count,
                s.Sum(x => x.Minutos),
                sr.Sum(x => x.Series),
                Math.Round(sr.Sum(x => x.VolumeKg), 0),
                Media(s.Where(x => x.Esforco.HasValue).Select(x => (decimal)x.Esforco!.Value)),
                Media(p.Select(x => (decimal)x.Score)),
                Media(p.Where(x => x.SonoHoras > 0).Select(x => x.SonoHoras)),
                p.Count,
                Media(r.Select(x => x.Pontos)),
                r.Count,
                Ultima(a => a.PesoKg),
                Ultima(a => a.PercentualGordura),
                Ultima(a => a.CinturaCm));
        }

        // Semanas de segunda a domingo cobrindo o periodo atual.
        var semanas = new List<EvolucaoSemanaResponse>();
        var segunda = inicioAtual.AddDays(-(((int)inicioAtual.DayOfWeek + 6) % 7));
        for (var ini = segunda; ini <= hoje; ini = ini.AddDays(7))
        {
            var r = Resumo(ini, ini.AddDays(6));
            semanas.Add(new EvolucaoSemanaResponse(ini, r.Treinos, r.Minutos, r.Series, r.VolumeKg, r.EsforcoMedio,
                r.ProntidaoMedia, r.SonoMedio, r.CheckIns, r.AdesaoDieta, r.RefeicoesRegistradas));
        }

        var grupos = series
            .Where(x => x.Series > 0)
            .GroupBy(x => x.Grupo)
            .Select(g => new EvolucaoGrupoMuscularResponse(
                g.Key,
                g.Where(x => x.Dia >= inicioAtual).Sum(x => x.Series),
                g.Where(x => x.Dia < inicioAtual).Sum(x => x.Series)))
            .OrderByDescending(x => x.SeriesAtual)
            .ThenByDescending(x => x.SeriesAnterior)
            .ToList();

        return new EvolucaoPeriodoResponse(
            dias,
            Resumo(inicioAtual, hoje),
            Resumo(inicioAnterior, fimAnterior),
            semanas,
            grupos,
            avaliacoes,
            fotos);
    }

    /// <summary>A foto e gravada com a URL da ficha; o paciente baixa pela rota do portal.</summary>
    public static string UrlDoPaciente(string url) => $"/api/portal/me/fotos-evolucao/{Path.GetFileName(url)}";

    private static string Pose(string tipo)
    {
        var i = tipo.IndexOf(':');
        return i >= 0 && i < tipo.Length - 1 ? tipo[(i + 1)..] : "Frente";
    }

    private static string Grupo(string? grupo) => string.IsNullOrWhiteSpace(grupo) ? "Outros" : grupo.Trim();

    private static DateTime Utc(DateOnly d) => DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static decimal? Media(IEnumerable<decimal> valores)
    {
        var lista = valores.ToList();
        return lista.Count == 0 ? null : Math.Round(lista.Average(), 1);
    }

    /// <summary>Carga x repeticoes, em kg. Libras sao convertidas; outras unidades (%, elastico) ficam de fora.</summary>
    private static decimal Volume(decimal? carga, string? unidade, int? series, string? repeticoes)
    {
        if (!carga.HasValue || carga.Value <= 0m) return 0m;
        var u = (unidade ?? "kg").Trim().ToLowerInvariant();
        var fator = u is "" or "kg" or "kgs" ? 1m : u is "lb" or "lbs" ? 0.4536m : 0m;
        if (fator == 0m || string.IsNullOrWhiteSpace(repeticoes)) return 0m;
        var numeros = Numeros().Matches(repeticoes).Select(x => int.Parse(x.Value)).ToList();
        if (numeros.Count == 0) return 0m;
        // "12,10,8" sao as repeticoes de cada serie; "10" ou "8-12" valem para todas as series.
        var reps = repeticoes.IndexOfAny([',', ';', '/']) >= 0 ? numeros.Sum() : numeros[0] * Math.Max(1, series ?? 1);
        return carga.Value * fator * reps;
    }

    [GeneratedRegex(@"\d{1,4}")]
    private static partial Regex Numeros();
}
