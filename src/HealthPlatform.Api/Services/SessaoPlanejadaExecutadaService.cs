using System.Globalization;
using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class SessaoPlanejadaExecutadaService
{
    public static async Task<PortalSessaoPlanejadaExecutadaResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia,
        PortalEstrategiaDiaResponse estrategia,
        PortalDisponibilidadeTreinoResponse disponibilidade,
        CancellationToken ct)
    {
        // Compara o que estava planejado com o que foi realmente executado sem transformar desvio em falha.
        // Adaptacao coerente com recuperacao/dor/carga e parte valida da execucao; intensidade extra nao recebe premio automatico.
        var inicioUtc = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);

        var execucao = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.SessaoTreino).ThenInclude(x => x.Itens)
            .Include(x => x.Itens).ThenInclude(x => x.ItemTreino)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" && x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .FirstOrDefaultAsync(ct);

        var sessaoPlanejada = estrategia.SessoesPrevistas.FirstOrDefault();
        if (execucao is null)
        {
            return new PortalSessaoPlanejadaExecutadaResponse(
                "SemExecucao", "Sessao planejada x executada",
                "Ainda nao ha sessao concluida registrada neste dia para comparar com o planejamento.",
                sessaoPlanejada, null, null, estrategia.RpeMin, estrategia.RpeMax, null,
                0, 0, null, null, null, false,
                Array.Empty<PortalComparacaoSessaoItemResponse>(),
                "Ausencia de execucao registrada nao deve ser interpretada automaticamente como baixa adesao.",
                "A comparacao descreve registro e contexto; nao diagnostica, nao pune adaptacoes e nao prescreve compensacao.");
        }

        var itensPlanejados = execucao.SessaoTreino?.Itens?.OrderBy(x => x.Ordem).ToList() ?? new List<HealthPlatform.Domain.Entities.ItemTreino>();
        var itensExecutados = execucao.Itens?.ToList() ?? new List<HealthPlatform.Domain.Entities.ExecucaoItemTreino>();
        var seriesPlanejadas = itensPlanejados.Sum(x => Math.Max(0, x.Series));
        var seriesExecutadas = itensExecutados.Where(x => x.Concluido).Sum(x => Math.Max(0, x.SeriesRealizadas ?? 0));
        var volumePlanejado = SomarVolumePlanejado(itensPlanejados);
        var volumeExecutado = SomarVolumeExecutado(itensExecutados);
        var rpe = execucao.EsforcoPercebido;
        var nomeExecutado = execucao.SessaoTreino?.Nome;

        var comparacoes = new List<PortalComparacaoSessaoItemResponse>();
        comparacoes.Add(new("sessao", "Sessao", NormalizarNome(sessaoPlanejada) == NormalizarNome(nomeExecutado) ? "Alinhada" : "Diferente",
            sessaoPlanejada ?? "Sem sessao especifica", nomeExecutado ?? "Sem nome",
            "Compara a sessao prevista na estrategia com a sessao registrada na execucao."));

        var estadoRpe = !rpe.HasValue ? "SemDado" : rpe.Value < estrategia.RpeMin ? "AbaixoDaFaixa" : rpe.Value > estrategia.RpeMax ? "AcimaDaFaixa" : "NaFaixa";
        comparacoes.Add(new("rpe", "Esforco percebido", estadoRpe,
            $"RPE {estrategia.RpeMin}-{estrategia.RpeMax}", rpe.HasValue ? $"RPE {rpe.Value}" : "Nao informado",
            "RPE fora da faixa e uma diferenca observada, nao um julgamento automatico de qualidade."));

        var estadoSeries = seriesPlanejadas == 0 ? "SemReferencia" : seriesExecutadas == seriesPlanejadas ? "Alinhada" : seriesExecutadas < seriesPlanejadas ? "Reduzida" : "Ampliada";
        comparacoes.Add(new("series", "Series", estadoSeries, seriesPlanejadas.ToString(CultureInfo.InvariantCulture), seriesExecutadas.ToString(CultureInfo.InvariantCulture),
            "Reducao pode representar adaptacao apropriada; aumento nao e automaticamente considerado melhor."));

        if (volumePlanejado.HasValue || volumeExecutado.HasValue)
        {
            var estadoVolume = !volumePlanejado.HasValue || !volumeExecutado.HasValue ? "Parcial" :
                volumeExecutado.Value > volumePlanejado.Value * 1.20m ? "AcimaDaReferencia" :
                volumeExecutado.Value < volumePlanejado.Value * 0.80m ? "AbaixoDaReferencia" : "ProximoDaReferencia";
            comparacoes.Add(new("volume", "Volume estimado", estadoVolume,
                volumePlanejado.HasValue ? volumePlanejado.Value.ToString("0.#", CultureInfo.InvariantCulture) : "Sem estimativa",
                volumeExecutado.HasValue ? volumeExecutado.Value.ToString("0.#", CultureInfo.InvariantCulture) : "Sem estimativa",
                "Volume estimado usa apenas itens com series, repeticoes numericas e carga disponiveis; nao e score de desempenho."));
        }

        var adaptacaoEsperada = disponibilidade.ExigeAdaptacao || estrategia.AjusteCargaPercentual < 0 || estrategia.AjusteVolumePercentual < 0;
        var reducaoObservada = (rpe.HasValue && rpe.Value < estrategia.RpeMin) || (seriesPlanejadas > 0 && seriesExecutadas < seriesPlanejadas) ||
                               (volumePlanejado.HasValue && volumeExecutado.HasValue && volumeExecutado.Value < volumePlanejado.Value * 0.80m);
        var acimaPlanejado = (rpe.HasValue && rpe.Value > estrategia.RpeMax) || (seriesPlanejadas > 0 && seriesExecutadas > seriesPlanejadas) ||
                             (volumePlanejado.HasValue && volumeExecutado.HasValue && volumeExecutado.Value > volumePlanejado.Value * 1.20m);
        var adaptacaoCoerente = adaptacaoEsperada && reducaoObservada && !acimaPlanejado;

        var estado = adaptacaoCoerente ? "AdaptadaAoContexto" : acimaPlanejado ? "AcimaDoPlanejado" :
            reducaoObservada ? "Adaptada" : "AlinhadaAoPlanejado";
        var resumo = estado switch
        {
            "AdaptadaAoContexto" => "A execucao foi reduzida em relacao a referencia e essa adaptacao e coerente com o contexto registrado do dia.",
            "AcimaDoPlanejado" => "A execucao ultrapassou pelo menos uma referencia planejada; isso deve ser contextualizado, nao premiado automaticamente.",
            "Adaptada" => "A execucao foi adaptada em relacao ao planejado. Adaptacao nao significa falha de adesao.",
            _ => "A execucao ficou proxima das referencias planejadas registradas para o dia."
        };

        var motivo = string.IsNullOrWhiteSpace(execucao.Observacoes) ? null : execucao.Observacoes.Trim();
        var leitura = motivo is not null
            ? $"Motivo/observacao registrado na execucao: {motivo}"
            : adaptacaoCoerente
                ? "A adaptacao e coerente com o contexto disponivel, mas nao ha motivo textual registrado; nao inventar motivo de adaptacao."
                : "Nao ha motivo textual registrado para eventuais diferencas; o sistema nao deve inferir causa automaticamente.";

        return new PortalSessaoPlanejadaExecutadaResponse(
            estado, "Sessao planejada x executada", resumo, sessaoPlanejada, nomeExecutado,
            rpe, estrategia.RpeMin, estrategia.RpeMax, execucao.DuracaoMinutos,
            seriesPlanejadas, seriesExecutadas, volumePlanejado, volumeExecutado,
            motivo, adaptacaoCoerente, comparacoes, leitura,
            "Planejado x executado e uma comparacao descritiva: adaptacao coerente nao e falha, execucao acima nao e premio, e o sistema nao prescreve compensacao nem aumento automatico.");
    }

    private static string NormalizarNome(string? valor) => (valor ?? string.Empty).Trim().ToUpperInvariant();

    private static int? RepeticoesNumericas(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var matches = Regex.Matches(texto, @"\d+").Cast<Match>().Select(x => int.Parse(x.Value, CultureInfo.InvariantCulture)).ToArray();
        if (matches.Length == 0) return null;
        return matches.Length == 1 ? matches[0] : (int)Math.Round(matches.Average());
    }

    private static decimal? SomarVolumePlanejado(IEnumerable<HealthPlatform.Domain.Entities.ItemTreino> itens)
    {
        decimal total = 0; var tem = false;
        foreach (var item in itens)
        {
            var reps = RepeticoesNumericas(item.Repeticoes);
            if (!reps.HasValue || !item.Carga.HasValue) continue;
            total += Math.Max(0, item.Series) * reps.Value * Math.Max(0, item.Carga.Value); tem = true;
        }
        return tem ? Math.Round(total, 1) : null;
    }

    private static decimal? SomarVolumeExecutado(IEnumerable<HealthPlatform.Domain.Entities.ExecucaoItemTreino> itens)
    {
        decimal total = 0; var tem = false;
        foreach (var item in itens.Where(x => x.Concluido))
        {
            var reps = RepeticoesNumericas(item.RepeticoesRealizadas);
            if (!reps.HasValue || !item.CargaRealizada.HasValue || !item.SeriesRealizadas.HasValue) continue;
            total += Math.Max(0, item.SeriesRealizadas.Value) * reps.Value * Math.Max(0, item.CargaRealizada.Value); tem = true;
        }
        return tem ? Math.Round(total, 1) : null;
    }
}
