using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class CargaTreinoService
{
    public static async Task<PortalCargaTreinoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        const int diasObservados = 28;
        var inicio28 = dia.AddDays(-27);
        var inicio7 = dia.AddDays(-6);
        var inicioUtc = DateTime.SpecifyKind(inicio28.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var inicio7Utc = DateTime.SpecifyKind(inicio7.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .Select(x => new { x.DataHoraInicioUtc, x.DuracaoMinutos, x.EsforcoPercebido })
            .OrderBy(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var recentes = treinos.Where(x => x.DataHoraInicioUtc >= inicio7Utc).ToList();
        var baseAnterior = treinos.Where(x => x.DataHoraInicioUtc < inicio7Utc).ToList();

        static decimal? CargaSessao(int? duracao, int? rpe)
            => duracao is > 0 && rpe is >= 1 and <= 10 ? duracao.Value * rpe.Value : null;

        var cargasRecentes = recentes.Select(x => CargaSessao(x.DuracaoMinutos, x.EsforcoPercebido)).Where(x => x.HasValue).Select(x => x!.Value).ToList();

        // Linha de base explicita: tres semanas completas anteriores aos 7 dias atuais.
        // Cada janela semanal pode legitimamente ter carga zero (deload/descanso); a media
        // continua representando o comportamento real das tres semanas anteriores.
        var cargasSemanaisBase = Enumerable.Range(0, 3)
            .Select(semana =>
            {
                var fimSemanaExclusivo = inicio7Utc.AddDays(-(semana * 7));
                var inicioSemana = fimSemanaExclusivo.AddDays(-7);
                return baseAnterior
                    .Where(x => x.DataHoraInicioUtc >= inicioSemana && x.DataHoraInicioUtc < fimSemanaExclusivo)
                    .Select(x => CargaSessao(x.DuracaoMinutos, x.EsforcoPercebido))
                    .Where(x => x.HasValue)
                    .Sum(x => x!.Value);
            })
            .ToList();

        decimal? carga7 = cargasRecentes.Count > 0 ? Math.Round(cargasRecentes.Sum(), 1) : null;
        decimal? cargaBaseSemanal = baseAnterior.Count > 0 ? Math.Round(cargasSemanaisBase.Sum() / 3m, 1) : null;
        decimal? RelacaoCargaComBase = carga7.HasValue && cargaBaseSemanal is > 0m
            ? Math.Round(carga7.Value / cargaBaseSemanal.Value, 2) : null;

        var rpes = recentes.Where(x => x.EsforcoPercebido.HasValue).Select(x => x.EsforcoPercebido!.Value).ToList();
        decimal? rpeMedio = rpes.Count > 0 ? Math.Round((decimal)rpes.Average(), 1) : null;
        var duracao7 = recentes.Where(x => x.DuracaoMinutos.HasValue).Sum(x => x.DuracaoMinutos ?? 0);
        int? diasDesdeUltimo = treinos.Count == 0 ? null : Math.Max(0, dia.DayNumber - DateOnly.FromDateTime(treinos[^1].DataHoraInicioUtc).DayNumber);
        var intensos = recentes.Count(x => x.EsforcoPercebido >= 8);

        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio7 && x.Data <= dia)
            .Select(x => new { x.Score, x.DorNivel, x.RecuperacaoNivel })
            .ToListAsync(ct);
        decimal? prontidaoMedia = prontidoes.Count > 0 ? Math.Round((decimal)prontidoes.Average(x => x.Score), 1) : null;
        decimal? dorMedia = prontidoes.Count > 0 ? Math.Round((decimal)prontidoes.Average(x => x.DorNivel), 1) : null;
        decimal? recuperacaoMedia = prontidoes.Count > 0 ? Math.Round((decimal)prontidoes.Average(x => x.RecuperacaoNivel), 1) : null;

        var sinais = new List<string>();
        if (intensos >= 3) sinais.Add($"{intensos} sessões com RPE 8+ nos últimos 7 dias.");
        if (rpeMedio >= 8m) sinais.Add($"RPE médio recente elevado ({rpeMedio:0.0}/10).");
        if (prontidaoMedia < 60m) sinais.Add($"Prontidão média recente em {prontidaoMedia:0.0}/100.");
        if (dorMedia >= 5m) sinais.Add($"Dor média recente em {dorMedia:0.0}/10.");
        if (recuperacaoMedia < 6m) sinais.Add($"Recuperação média recente em {recuperacaoMedia:0.0}/10.");

        string classificacao;
        string nivel;
        string mensagem;
        if (recentes.Count < 1 || !carga7.HasValue)
        {
            classificacao = "DadosInsuficientes"; nivel = "Info";
            mensagem = "Registre duração e RPE das sessões para acompanhar a carga interna semanal.";
        }
        else if (!RelacaoCargaComBase.HasValue)
        {
            classificacao = "ConstruindoBase"; nivel = "Baixa";
            mensagem = "A carga desta semana já pode ser acompanhada; faltam semanas anteriores para uma linha de base comparável.";
        }
        else if (RelacaoCargaComBase.Value > 1.60m || (RelacaoCargaComBase.Value > 1.30m && (prontidaoMedia < 60m || dorMedia >= 5m)))
        {
            classificacao = "Revisar"; nivel = "Alta";
            mensagem = "A carga recente está bem acima da linha de base ou combinada com recuperação desfavorável. Revise o planejamento antes de progredir.";
        }
        else if (RelacaoCargaComBase.Value > 1.30m)
        {
            classificacao = "AcimaDaBase"; nivel = "Media";
            mensagem = "A carga recente está acima da linha de base. Interprete junto do ciclo, da prontidão e da recuperação.";
        }
        else if (RelacaoCargaComBase.Value < 0.70m)
        {
            classificacao = "AbaixoDaBase"; nivel = "Baixa";
            mensagem = "A carga recente está abaixo da linha de base; isso pode ser coerente com recuperação, deload ou mudança planejada do ciclo.";
        }
        else
        {
            classificacao = "Coerente"; nivel = "Baixa";
            mensagem = "A carga interna recente está próxima da linha de base das semanas anteriores.";
        }

        return new PortalCargaTreinoResponse(
            diasObservados, recentes.Count, intensos, duracao7 > 0 ? duracao7 : null,
            carga7, cargaBaseSemanal, RelacaoCargaComBase, rpeMedio, diasDesdeUltimo, classificacao, nivel, mensagem, sinais);
    }
}
