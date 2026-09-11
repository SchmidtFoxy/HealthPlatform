using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class TendenciaRecuperacaoService
{
    public static async Task<PortalTendenciaRecuperacaoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var inicio14 = dia.AddDays(-13);
        var inicio7 = dia.AddDays(-6);
        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio14 && x.Data <= dia)
            .OrderBy(x => x.Data)
            .ToListAsync(ct);

        var atual = prontidoes.Where(x => x.Data >= inicio7).ToList();
        var anterior = prontidoes.Where(x => x.Data < inicio7).ToList();

        var inicioUtc = DateTime.SpecifyKind(inicio7.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .Select(x => new { x.EsforcoPercebido })
            .ToListAsync(ct);

        decimal? Media(IEnumerable<int> valores)
        {
            var lista = valores.ToList();
            return lista.Count == 0 ? null : Math.Round((decimal)lista.Average(), 1);
        }
        decimal? MediaDecimal(IEnumerable<decimal> valores)
        {
            var lista = valores.ToList();
            return lista.Count == 0 ? null : Math.Round(lista.Average(), 1);
        }

        var prontidaoMedia = Media(atual.Select(x => x.Score));
        var prontidaoAnterior = Media(anterior.Select(x => x.Score));
        var variacao = prontidaoMedia.HasValue && prontidaoAnterior.HasValue
            ? Math.Round(prontidaoMedia.Value - prontidaoAnterior.Value, 1) : (decimal?)null;
        var sonoMedio = MediaDecimal(atual.Select(x => x.SonoHoras));
        var dorMedia = Media(atual.Select(x => x.DorNivel));
        var recuperacaoMedia = Media(atual.Select(x => x.RecuperacaoNivel));
        var energiaMedia = Media(atual.Select(x => x.EnergiaNivel));
        var treinosIntensos = treinos.Count(x => x.EsforcoPercebido >= 8);

        var sinais = new List<PortalSinalRecuperacaoResponse>();
        if (atual.Count < 3)
        {
            sinais.Add(new("dados-insuficientes", "Info", "Poucos check-ins recentes",
                "Faça pelo menos 3 check-ins na semana para formar uma tendência mais confiável."));
            return new PortalTendenciaRecuperacaoResponse(
                atual.Count, prontidaoMedia, prontidaoAnterior, variacao, sonoMedio, dorMedia,
                recuperacaoMedia, energiaMedia, treinos.Count, treinosIntensos, "DadosInsuficientes", "Info",
                "Ainda não há dados suficientes para interpretar uma tendência de recuperação.", sinais);
        }

        if (dorMedia >= 6m)
            sinais.Add(new("dor-elevada", "Alta", "Dor elevada nos últimos dias",
                $"Dor média de {dorMedia:0.0}/10 nos check-ins recentes. Vale revisar carga e sintomas com o profissional."));
        else if (dorMedia >= 4.5m)
            sinais.Add(new("dor-observar", "Media", "Dor merece observação",
                $"Dor média de {dorMedia:0.0}/10. Observe se ela está concentrada após sessões específicas."));

        if (sonoMedio < 6m)
            sinais.Add(new("sono-baixo", "Alta", "Sono abaixo do desejável",
                $"Média de {sonoMedio:0.0}h de sono. Recuperação e qualidade do treino podem ser impactadas."));
        else if (sonoMedio < 7m)
            sinais.Add(new("sono-observar", "Media", "Sono pode melhorar",
                $"Média de {sonoMedio:0.0}h. Priorizar regularidade pode favorecer a recuperação."));

        if (recuperacaoMedia <= 4.5m || prontidaoMedia < 55m)
            sinais.Add(new("recuperacao-baixa", "Alta", "Recuperação abaixo do padrão",
                $"Recuperação média {recuperacaoMedia:0.0}/10 e prontidão média {prontidaoMedia:0.0}/100."));
        else if (recuperacaoMedia < 6m || prontidaoMedia < 65m)
            sinais.Add(new("recuperacao-observar", "Media", "Recuperação oscilando",
                $"Prontidão média {prontidaoMedia:0.0}/100. Acompanhe a resposta aos próximos treinos."));

        if (treinosIntensos >= 3 && recuperacaoMedia < 7m)
            sinais.Add(new("carga-intensa-recente", "Media", "Carga intensa recente",
                $"Foram {treinosIntensos} sessões com RPE 8+ em 7 dias, junto de recuperação média {recuperacaoMedia:0.0}/10."));

        if (variacao <= -8m)
            sinais.Add(new("prontidao-queda", "Media", "Prontidão em queda",
                $"A média caiu {Math.Abs(variacao.Value):0.0} pontos em relação aos 7 dias anteriores."));
        else if (variacao >= 8m)
            sinais.Add(new("prontidao-melhora", "Positivo", "Prontidão em melhora",
                $"A média subiu {variacao.Value:0.0} pontos em relação aos 7 dias anteriores."));

        var atencaoAlta = sinais.Any(x => x.Severidade == "Alta");
        var atencaoMedia = sinais.Any(x => x.Severidade == "Media");
        var tendencia = atencaoAlta ? "Atencao" : atencaoMedia ? "Observar" : variacao >= 8m ? "Melhorando" : "Estavel";
        var nivel = atencaoAlta ? "Alta" : atencaoMedia ? "Media" : "Baixa";
        var mensagem = tendencia switch
        {
            "Atencao" => "Há sinais de recuperação que merecem revisão antes de aumentar a carga.",
            "Observar" => "A recuperação apresenta oscilações. Mantenha o acompanhamento dos próximos dias.",
            "Melhorando" => "A tendência recente de recuperação está melhorando; mantenha a estratégia planejada.",
            _ => "A recuperação está relativamente estável nos registros recentes."
        };

        return new PortalTendenciaRecuperacaoResponse(
            atual.Count, prontidaoMedia, prontidaoAnterior, variacao, sonoMedio, dorMedia,
            recuperacaoMedia, energiaMedia, treinos.Count, treinosIntensos, tendencia, nivel, mensagem, sinais);
    }
}
