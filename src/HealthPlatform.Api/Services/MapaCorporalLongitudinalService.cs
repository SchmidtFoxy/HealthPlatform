using System.Text.Json;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class MapaCorporalLongitudinalService
{
    private sealed record DorPayload(string Regiao, string? Lado, int ImpactoTreino, string? Observacao);
    private sealed record DorLinha(DateOnly Data, string Regiao, string? Lado, int Intensidade, int ImpactoTreino);

    public static async Task<PortalMapaCorporalLongitudinalResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        // Mapa longitudinal ancorado apenas em registros DorCorporal realmente persistidos.
        // A janela de 56 dias permite comparar 28 dias recentes vs. 28 dias anteriores sem criar diagnostico retrospectivo.
        const int periodoDias = 56;
        var fim = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var inicio = fim.AddDays(-periodoDias);
        var corte28 = DateOnly.FromDateTime(fim.AddDays(-28));

        var rows = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "DorCorporal" &&
                        x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .OrderBy(x => x.DataHoraUtc)
            .ToListAsync(ct);

        var linhas = new List<DorLinha>();
        foreach (var row in rows)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<DorPayload>(row.Descricao ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (payload is null || string.IsNullOrWhiteSpace(payload.Regiao)) continue;
                linhas.Add(new DorLinha(
                    DateOnly.FromDateTime(row.DataHoraUtc), payload.Regiao.Trim(), NormalizarLado(payload.Lado),
                    Math.Clamp(row.Escala ?? 0, 0, 10), Math.Clamp(payload.ImpactoTreino, 0, 10)));
            }
            catch (JsonException) { }
        }

        var regioes = linhas
            .GroupBy(x => new { Regiao = x.Regiao.ToUpperInvariant(), Lado = (x.Lado ?? "").ToUpperInvariant() })
            .Select(g =>
            {
                var ordenados = g.OrderBy(x => x.Data).ToList();
                var dias = ordenados.Select(x => x.Data).Distinct().OrderBy(x => x).ToList();
                var recentes = dias.Count(x => x >= corte28);
                var anteriores = dias.Count - recentes;
                var recorrente = dias.Count >= 3;
                var IntensidadeMaxima = ordenados.Max(x => x.Intensidade);
                var IntensidadeMedia = Math.Round(ordenados.Average(x => (decimal)x.Intensidade), 1);
                var ImpactoMaximoTreino = ordenados.Max(x => x.ImpactoTreino);
                var estado = IntensidadeMaxima >= 7 || ImpactoMaximoTreino >= 7 ? "Acompanhar" : recorrente ? "Recorrente" : "Observado";
                var tendencia = recentes > anteriores ? "MaisPresente" : recentes < anteriores ? "MenosPresente" : "SemMudanca";
                if (dias.Count < 2) tendencia = "HistoricoCurto";

                return new PortalMapaCorporalLongitudinalRegiaoResponse(
                    ordenados[0].Regiao, ordenados[0].Lado, ordenados.Count, dias.Count, recentes, anteriores,
                    IntensidadeMaxima, IntensidadeMedia, ImpactoMaximoTreino,
                    dias[0], dias[^1], estado, tendencia, dias.OrderByDescending(x => x).Take(8).ToList());
            })
            .OrderByDescending(x => x.Estado == "Acompanhar")
            .ThenByDescending(x => x.DiasComRegistro)
            .ThenByDescending(x => x.IntensidadeMaxima)
            .ThenBy(x => x.Regiao)
            .ToList();

        if (regioes.Count == 0)
            return new PortalMapaCorporalLongitudinalResponse(
                "SemDados", "Mapa corporal longitudinal", "Ainda nao ha registros localizados nos ultimos 56 dias.",
                periodoDias, 0, 0, 0, regioes,
                "Ausencia de registro nao significa ausencia de dor. O mapa nao produz diagnostico, nao estima risco de lesao e nao prova causa entre treino e sintoma.");

        var recorrentes = regioes.Count(x => x.DiasComRegistro >= 3);
        var atencao = regioes.Count(x => x.Estado == "Acompanhar");
        var estadoGeral = atencao > 0 ? "Acompanhar" : recorrentes > 0 ? "RecorrenciaObservada" : "HistoricoDisponivel";
        var resumo = atencao > 0
            ? $"{atencao} regiao(oes) combina(m) intensidade/impacto que merece(m) revisao profissional no contexto esportivo."
            : recorrentes > 0
                ? $"{recorrentes} regiao(oes) apareceu(ram) em tres ou mais dias na janela de 56 dias."
                : "Ha registros localizados suficientes para visualizar distribuicao temporal, ainda sem recorrencia definida.";

        return new PortalMapaCorporalLongitudinalResponse(
            estadoGeral, "Mapa corporal longitudinal", resumo, periodoDias, linhas.Count, regioes.Count, recorrentes, regioes,
            "Recorrencia descreve frequencia observada: nao e diagnostico, nao calcula probabilidade de lesao, nao prova causalidade e nao prescreve conduta automaticamente.");
    }

    private static string? NormalizarLado(string? lado)
    {
        if (string.IsNullOrWhiteSpace(lado)) return null;
        var valor = lado.Trim();
        return valor.Length > 24 ? valor[..24] : valor;
    }
}
