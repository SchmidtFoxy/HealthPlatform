using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class ComparativoCiclosService
{
    public static async Task<PortalComparativoCiclosResponse> MontarAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        CancellationToken ct)
    {
        var ciclos = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.DataInicio <= dia && x.Status != "Planejado")
            .OrderByDescending(x => x.DataInicio)
            .Take(4)
            .ToListAsync(ct);

        if (ciclos.Count == 0)
            return new("SemDados", "Comparativo de ciclos", "Ainda não há ciclos esportivos iniciados para comparar.",
                Array.Empty<PortalComparativoCicloItemResponse>(), null,
                "A comparação é descritiva; não cria score global, diagnóstico ou ajuste automático de prescrição.");

        var itens = new List<PortalComparativoCicloItemResponse>();
        foreach (var ciclo in ciclos.OrderBy(x => x.DataInicio))
        {
            var fimObservado = ciclo.DataFim < dia ? ciclo.DataFim : dia;
            if (fimObservado < ciclo.DataInicio) continue;

            var inicioUtc = ciclo.DataInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var fimUtc = fimObservado.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var diasObservados = Math.Max(1, fimObservado.DayNumber - ciclo.DataInicio.DayNumber + 1);
            var semanasObservadas = Math.Max(1m, diasObservados / 7m);

            var treinos = await db.ExecucoesTreino.AsNoTracking()
                .CountAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                                 x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc, ct);

            var checkinsQuery = db.ProntidoesDiarias.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.Data >= ciclo.DataInicio && x.Data <= fimObservado);
            var checkins = await checkinsQuery.CountAsync(ct);
            var mediaProntidao = checkins == 0
                ? (decimal?)null
                : Math.Round(await checkinsQuery.AverageAsync(x => (decimal)x.Score, ct), 1);

            var pesos = await db.Avaliacoes.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.PesoKg.HasValue &&
                            x.DataUtc >= inicioUtc && x.DataUtc < fimUtc)
                .OrderBy(x => x.DataUtc)
                .Select(x => x.PesoKg!.Value)
                .ToListAsync(ct);

            var pesoInicial = pesos.Count > 0 ? pesos[0] : (decimal?)null;
            var pesoFinal = pesos.Count > 0 ? pesos[^1] : (decimal?)null;
            var variacaoPeso = pesoInicial.HasValue && pesoFinal.HasValue
                ? Math.Round(pesoFinal.Value - pesoInicial.Value, 1)
                : (decimal?)null;

            itens.Add(new PortalComparativoCicloItemResponse(
                ciclo.Id, ciclo.Nome, ciclo.PerfilEsportivo, ciclo.Status, ciclo.DataInicio, ciclo.DataFim,
                diasObservados,
                treinos,
                Math.Round(treinos / semanasObservadas, 2),
                checkins,
                Math.Round(checkins / semanasObservadas, 2),
                mediaProntidao,
                pesoInicial,
                pesoFinal,
                variacaoPeso));
        }

        itens = itens.OrderByDescending(x => x.DataInicio).ToList();
        if (itens.Count == 0)
            return new("SemDados", "Comparativo de ciclos", "Ainda não há dados observáveis nos ciclos encontrados.", itens, null,
                "A comparação é descritiva; não cria score global, diagnóstico ou ajuste automático de prescrição.");

        string? comparacaoAnterior = null;
        var estado = itens.Count >= 2 ? "Comparavel" : "PrimeiroCiclo";
        if (itens.Count >= 2)
        {
            var atual = itens[0];
            var anterior = itens[1];
            var deltaTreinos = Math.Round(atual.TreinosPorSemana - anterior.TreinosPorSemana, 2);
            var deltaCheckins = Math.Round(atual.CheckInsPorSemana - anterior.CheckInsPorSemana, 2);
            var deltaProntidao = atual.MediaProntidao.HasValue && anterior.MediaProntidao.HasValue
                ? Math.Round(atual.MediaProntidao.Value - anterior.MediaProntidao.Value, 1)
                : (decimal?)null;

            comparacaoAnterior = $"Vs. ciclo anterior: treinos/semana {FormatarDelta(deltaTreinos)}, check-ins/semana {FormatarDelta(deltaCheckins)}" +
                (deltaProntidao.HasValue ? $", prontidão média {FormatarDelta(deltaProntidao.Value)} ponto(s)." : ".");
        }

        var resumo = itens.Count >= 2
            ? $"{itens.Count} ciclo(s) disponíveis. Compare ritmo, prontidão e medidas sem transformar diferenças em julgamento automático."
            : "Este é o primeiro ciclo com dados suficientes; ele servirá como referência para comparações futuras.";

        return new(estado, "Comparativo de ciclos", resumo, itens, comparacaoAnterior,
            "Ciclos têm objetivos e durações diferentes. A comparação usa taxas semanais e evidências separadas; variação de peso não é classificada como boa ou ruim automaticamente.");
    }

    private static string FormatarDelta(decimal valor)
        => valor > 0 ? $"+{valor:0.##}" : $"{valor:0.##}";
}
