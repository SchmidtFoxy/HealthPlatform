using System.Text.Json;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Compara a semana atual com o mesmo número de dias da semana anterior.
/// A leitura é descritiva: aumento de carga, peso ou volume não é classificado automaticamente como melhor/pior.
/// </summary>
public static class TendenciaSemanalService
{
    private sealed record JanelaResumo(
        int Treinos, int DiasAtivos, decimal? ProntidaoMedia, decimal? CargaInterna,
        decimal? AdesaoNutricionalMedia, decimal? HidratacaoMedia);

    public static async Task<PortalTendenciaSemanalResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var deslocamento = ((int)dia.DayOfWeek + 6) % 7;
        var atualInicio = dia.AddDays(-deslocamento);
        var atualFim = dia;
        var diasComparados = atualFim.DayNumber - atualInicio.DayNumber + 1;
        var anteriorInicio = atualInicio.AddDays(-7);
        var anteriorFim = anteriorInicio.AddDays(diasComparados - 1);

        var atual = await LerJanelaAsync(db, pacienteId, atualInicio, atualFim, ct);
        var anterior = await LerJanelaAsync(db, pacienteId, anteriorInicio, anteriorFim, ct);
        var itens = new List<PortalTendenciaSemanalItemResponse>
        {
            CompararInteiro("treinos", "Treino", "Ritmo de treinos", atual.Treinos, anterior.Treinos, "sessão(ões)", false),
            CompararInteiro("dias-ativos", "Consistência", "Dias ativos", atual.DiasAtivos, anterior.DiasAtivos, "dia(s)", false),
            CompararDecimal("prontidao", "Recuperação", "Prontidão média", atual.ProntidaoMedia, anterior.ProntidaoMedia, "/100", true, 2m),
            CompararDecimal("carga", "Carga", "Carga interna estimada", atual.CargaInterna, anterior.CargaInterna, "u.a.", false, 10m),
            CompararDecimal("nutricao", "Nutrição", "Adesão nutricional registrada", atual.AdesaoNutricionalMedia, anterior.AdesaoNutricionalMedia, "%", true, 5m),
            CompararDecimal("hidratacao", "Hidratação", "Hidratação registrada", atual.HidratacaoMedia, anterior.HidratacaoMedia, "%", true, 5m)
        };

        var comparaveis = itens.Count(x => x.Estado != "SemDados");
        var mudancas = itens.Count(x => x.Estado is "Subiu" or "Caiu" or "Maior" or "Menor");
        var estado = comparaveis < 3 ? "DadosInsuficientes" : mudancas >= 3 ? "MudancaRelevante" : "Estavel";
        var titulo = estado switch
        {
            "DadosInsuficientes" => "Tendência semanal em formação",
            "MudancaRelevante" => "A semana mudou em vários eixos",
            _ => "Semana semelhante à anterior"
        };
        var resumo = estado == "DadosInsuficientes"
            ? "Ainda faltam registros comparáveis entre esta semana e a anterior."
            : $"{comparaveis} eixo(s) comparáveis no mesmo intervalo de {diasComparados} dia(s); {mudancas} apresentam mudança relevante.";

        return new(estado, titulo, resumo, atualInicio, atualFim, anteriorInicio, anteriorFim, itens,
            "A comparação usa o mesmo número de dias em cada semana. Mudanças de carga, frequência ou medidas são descritivas e não autorizam ajuste automático de treino, nutrição ou medicação.");
    }

    private static async Task<JanelaResumo> LerJanelaAsync(
        AppDbContext db, Guid pacienteId, DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        var inicioUtc = DateTime.SpecifyKind(inicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(fim.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .Select(x => new { x.DuracaoMinutos, x.EsforcoPercebido })
            .ToListAsync(ct);
        decimal? carga = treinos.Any(x => x.DuracaoMinutos.HasValue && x.EsforcoPercebido.HasValue)
            ? treinos.Where(x => x.DuracaoMinutos.HasValue && x.EsforcoPercebido.HasValue)
                .Sum(x => (decimal)(x.DuracaoMinutos!.Value * x.EsforcoPercebido!.Value))
            : null;

        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio && x.Data <= fim)
            .Select(x => x.Score).ToListAsync(ct);
        decimal? prontidao = prontidoes.Count == 0 ? null : Math.Round(prontidoes.Average(x => (decimal)x), 1);

        var diasAtivos = await db.EventosXp.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio && x.Data <= fim)
            .Select(x => x.Data).Distinct().CountAsync(ct);

        var diarios = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "AdesaoRefeicao" &&
                        x.DataHoraUtc >= inicioUtc && x.DataHoraUtc < fimUtc)
            .Select(x => x.Descricao).ToListAsync(ct);
        var notasNutricao = diarios.Select(LerNotaNutricao).Where(x => x.HasValue).Select(x => x!.Value).ToList();
        decimal? nutricao = notasNutricao.Count == 0 ? null : Math.Round(notasNutricao.Average(), 1);

        decimal? hidratacao = null;
        var meta = await db.MetasPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativa" && x.ValorObjetivo.HasValue && x.ValorObjetivo > 0 &&
                        x.DataInicio <= fim && (!x.DataFim.HasValue || x.DataFim.Value >= inicio) &&
                        (x.Tipo.ToLower().Contains("hidrat") || x.Nome.ToLower().Contains("agua") || x.Nome.ToLower().Contains("água") || x.Nome.ToLower().Contains("hidrat")))
            .OrderByDescending(x => x.DataInicio).FirstOrDefaultAsync(ct);
        if (meta is not null)
        {
            var registros = await db.RegistrosMetas.AsNoTracking()
                .Where(x => x.MetaPacienteId == meta.Id && x.Data >= inicio && x.Data <= fim && x.Valor.HasValue)
                .Select(x => x.Valor!.Value)
                .ToListAsync(ct);
            if (registros.Count > 0)
                hidratacao = Math.Round(registros.Average(v => Math.Min(150m, v / meta.ValorObjetivo!.Value * 100m)), 1);
        }

        return new(treinos.Count, diasAtivos, prontidao, carga, nutricao, hidratacao);
    }

    private static decimal? LerNotaNutricao(string? descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao)) return null;
        try
        {
            using var doc = JsonDocument.Parse(descricao);
            if (!doc.RootElement.TryGetProperty("status", out var status)) return null;
            return (status.GetString() ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "realizada" => 100m,
                "adaptada" => 80m,
                "naorealizada" or "nao-realizada" or "não realizada" or "nao realizada" => 0m,
                _ => null
            };
        }
        catch { return null; }
    }

    private static PortalTendenciaSemanalItemResponse CompararInteiro(
        string codigo, string categoria, string titulo, int atual, int anterior, string unidade, bool maiorEhFavoravel)
    {
        var delta = atual - anterior;
        var estado = delta == 0 ? "Estavel" : delta > 0 ? "Maior" : "Menor";
        var leitura = delta == 0 ? "Mesmo ritmo no período comparado."
            : "Mudança de ritmo observada; interprete em conjunto com a estratégia e a recuperação.";
        return new(codigo, categoria, estado, titulo, $"{atual} {unidade}", $"{anterior} {unidade}", FormatarDelta(delta), leitura);
    }

    private static PortalTendenciaSemanalItemResponse CompararDecimal(
        string codigo, string categoria, string titulo, decimal? atual, decimal? anterior, string unidade,
        bool maiorEhFavoravel, decimal tolerancia)
    {
        if (!atual.HasValue || !anterior.HasValue)
            return new(codigo, categoria, "SemDados", titulo,
                atual.HasValue ? $"{atual:0.#}{unidade}" : "—", anterior.HasValue ? $"{anterior:0.#}{unidade}" : "—", "—",
                "São necessários registros nos dois períodos para comparar este eixo.");

        var delta = Math.Round(atual.Value - anterior.Value, 1);
        var estado = Math.Abs(delta) < tolerancia ? "Estavel" : delta > 0 ? (maiorEhFavoravel ? "Subiu" : "Maior") : (maiorEhFavoravel ? "Caiu" : "Menor");
        var leitura = categoria == "Carga"
            ? "Carga maior ou menor não é boa ou ruim isoladamente; compare com recuperação e planejamento."
            : estado == "Estavel" ? "Sem mudança relevante no intervalo comparado." : "Há mudança observável; use como contexto, não como diagnóstico.";
        return new(codigo, categoria, estado, titulo, $"{atual:0.#}{unidade}", $"{anterior:0.#}{unidade}", FormatarDelta(delta), leitura);
    }

    private static string FormatarDelta(decimal delta) => delta > 0 ? $"+{delta:0.#}" : $"{delta:0.#}";
}
