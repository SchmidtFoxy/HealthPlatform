using System.Text.Json;
using HealthPlatform.Api.Contracts.MonitoramentoPassivo;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class MonitoramentoPassivoService
{
    public const string TipoRegistro = "MonitoramentoPassivo";

    private sealed record DefinicaoMetrica(string Chave, string Rotulo, string UnidadePadrao, bool Somar);
    private sealed record MetadataSinal(string Fonte, string Metrica, string? IdExterno, DateTime InicioUtc, DateTime? FimUtc, string? Qualidade);
    private sealed record Leitura(RegistroDiarioPaciente Registro, MetadataSinal Metadata);

    private static readonly IReadOnlyDictionary<string, string> Fontes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["AppleHealth"] = "Apple Health",
        ["HealthConnect"] = "Health Connect",
        ["Garmin"] = "Garmin"
    };

    private static readonly IReadOnlyDictionary<string, DefinicaoMetrica> Metricas = new Dictionary<string, DefinicaoMetrica>(StringComparer.OrdinalIgnoreCase)
    {
        ["Passos"] = new("Passos", "Passos", "passos", true),
        ["SonoMinutos"] = new("SonoMinutos", "Sono", "min", false),
        ["FrequenciaCardiacaRepouso"] = new("FrequenciaCardiacaRepouso", "FC de repouso", "bpm", false),
        ["HRV"] = new("HRV", "Variabilidade da FC", "ms", false),
        ["EnergiaAtiva"] = new("EnergiaAtiva", "Energia ativa", "kcal", true),
        ["Distancia"] = new("Distancia", "Distancia", "km", true),
        ["MinutosAtivos"] = new("MinutosAtivos", "Minutos ativos", "min", true)
    };

    public static IReadOnlyList<string> FontesSuportadas => Fontes.Keys.OrderBy(x => x).ToArray();

    public static async Task<MonitoramentoPassivoImportacaoResponse> ImportarAsync(
        AppDbContext db,
        Guid pacienteId,
        MonitoramentoPassivoImportacaoRequest request,
        CancellationToken ct)
    {
        var sinais = request.Sinais?.Take(500).ToArray() ?? [];
        if (sinais.Length == 0)
            return new(0, 0, 0, 0, FontesSuportadas);

        var desde = DateTime.UtcNow.AddDays(-180);
        var existentes = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == TipoRegistro && x.DataHoraUtc >= desde)
            .Select(x => x.Descricao)
            .ToListAsync(ct);

        var chavesExistentes = existentes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(TentarLerMetadata)
            .Where(x => x is not null)
            .Cast<MetadataSinal>()
            .Select(ChaveDeduplicacao)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var importados = 0;
        var duplicados = 0;
        var rejeitados = 0;

        foreach (var sinal in sinais)
        {
            if (!TentarNormalizar(sinal, out var fonte, out var metrica, out var inicioUtc, out var fimUtc, out var valor, out var unidade, out var qualidade))
            {
                rejeitados++;
                continue;
            }

            var metadata = new MetadataSinal(fonte, metrica.Chave, LimparId(sinal.IdExterno), inicioUtc, fimUtc, qualidade);
            var chave = ChaveDeduplicacao(metadata);
            if (!chavesExistentes.Add(chave))
            {
                duplicados++;
                continue;
            }

            db.RegistrosDiarioPaciente.Add(new RegistroDiarioPaciente
            {
                PacienteId = pacienteId,
                DataHoraUtc = inicioUtc,
                Tipo = TipoRegistro,
                Descricao = JsonSerializer.Serialize(metadata),
                ValorNumerico = valor,
                Unidade = unidade
            });
            importados++;
        }

        if (importados > 0)
            await db.SaveChangesAsync(ct);

        return new(sinais.Length, importados, duplicados, rejeitados, FontesSuportadas);
    }

    public static async Task<MonitoramentoPassivoResumoResponse> ResumirAsync(
        AppDbContext db,
        Guid pacienteId,
        int dias,
        CancellationToken ct)
    {
        dias = Math.Clamp(dias, 1, 90);
        var agora = DateTime.UtcNow;
        var desde = agora.AddDays(-Math.Max(dias, 14));
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == TipoRegistro && x.DataHoraUtc >= desde)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(5000)
            .ToListAsync(ct);

        var leituras = registros
            .Select(x => new Leitura(x, TentarLerMetadata(x.Descricao) ?? new MetadataSinal("Desconhecida", "Desconhecida", null, x.DataHoraUtc, null, null)))
            .Where(x => Fontes.ContainsKey(x.Metadata.Fonte) && Metricas.ContainsKey(x.Metadata.Metrica))
            .ToArray();

        var janela = agora.AddDays(-dias);
        var atuais = leituras.Where(x => x.Registro.DataHoraUtc >= janela).ToArray();
        var inicio7 = agora.AddDays(-7);
        var inicio14 = agora.AddDays(-14);

        var fontes = Fontes.Select(par =>
        {
            var itens = atuais.Where(x => string.Equals(x.Metadata.Fonte, par.Key, StringComparison.OrdinalIgnoreCase)).ToArray();
            var ultima = itens.Length == 0 ? (DateTime?)null : itens.Max(x => x.Registro.DataHoraUtc);
            return new MonitoramentoPassivoFonteResponse(par.Key, par.Value, itens.Length, ultima, ultima.HasValue && ultima.Value >= agora.AddHours(-36));
        }).ToArray();

        var metricas = Metricas.Values.Select(def =>
        {
            var itens = atuais.Where(x => string.Equals(x.Metadata.Metrica, def.Chave, StringComparison.OrdinalIgnoreCase) && x.Registro.ValorNumerico.HasValue).ToArray();
            var recente7 = leituras.Where(x => x.Registro.DataHoraUtc >= inicio7 && string.Equals(x.Metadata.Metrica, def.Chave, StringComparison.OrdinalIgnoreCase) && x.Registro.ValorNumerico.HasValue).ToArray();
            var anterior7 = leituras.Where(x => x.Registro.DataHoraUtc >= inicio14 && x.Registro.DataHoraUtc < inicio7 && string.Equals(x.Metadata.Metrica, def.Chave, StringComparison.OrdinalIgnoreCase) && x.Registro.ValorNumerico.HasValue).ToArray();
            var ultimo = itens.OrderByDescending(x => x.Registro.DataHoraUtc).FirstOrDefault();
            var media = itens.Length == 0 ? (decimal?)null : Math.Round(itens.Average(x => x.Registro.ValorNumerico!.Value), 1);
            var total = itens.Length == 0 || !def.Somar ? (decimal?)null : Math.Round(itens.Sum(x => x.Registro.ValorNumerico!.Value), 1);
            var atual7 = ValorJanela(recente7, def.Somar);
            var anterior = ValorJanela(anterior7, def.Somar);
            var variacao = anterior is null || anterior == 0 || atual7 is null ? (decimal?)null : Math.Round(((atual7.Value - anterior.Value) / Math.Abs(anterior.Value)) * 100m, 1);
            return new MonitoramentoPassivoMetricaResponse(def.Chave, def.Rotulo, def.UnidadePadrao, itens.Length,
                ultimo?.Registro.ValorNumerico, ultimo?.Registro.DataHoraUtc, media, total, variacao);
        }).Where(x => x.Amostras > 0).ToArray();

        return new MonitoramentoPassivoResumoResponse(
            pacienteId,
            dias,
            atuais.Length,
            atuais.Length == 0 ? null : atuais.Max(x => x.Registro.DataHoraUtc),
            fontes,
            metricas,
            "Dados passivos ajudam a observar tendencias. Eles nao geram diagnostico, prescricao ou mudanca automatica de conduta.");
    }

    private static decimal? ValorJanela(IReadOnlyCollection<Leitura> itens, bool somar)
    {
        if (itens.Count == 0) return null;
        return somar
            ? itens.Sum(x => x.Registro.ValorNumerico!.Value)
            : itens.Average(x => x.Registro.ValorNumerico!.Value);
    }

    private static bool TentarNormalizar(
        MonitoramentoPassivoSinalRequest sinal,
        out string fonte,
        out DefinicaoMetrica metrica,
        out DateTime inicioUtc,
        out DateTime? fimUtc,
        out decimal valor,
        out string unidade,
        out string? qualidade)
    {
        fonte = sinal.Fonte?.Trim() ?? string.Empty;
        metrica = null!;
        inicioUtc = default;
        fimUtc = null;
        valor = sinal.Valor;
        unidade = sinal.Unidade?.Trim() ?? string.Empty;
        qualidade = string.IsNullOrWhiteSpace(sinal.Qualidade) ? null : sinal.Qualidade.Trim()[..Math.Min(80, sinal.Qualidade.Trim().Length)];

        if (!Fontes.ContainsKey(fonte)) return false;
        if (!Metricas.TryGetValue(sinal.Metrica?.Trim() ?? string.Empty, out var metricaEncontrada)) return false;
        metrica = metricaEncontrada;
        if (sinal.InicioUtc == default) return false;
        if (valor < 0 || valor > 1_000_000m) return false;

        inicioUtc = sinal.InicioUtc.Kind == DateTimeKind.Utc ? sinal.InicioUtc : sinal.InicioUtc.ToUniversalTime();
        fimUtc = sinal.FimUtc.HasValue ? (sinal.FimUtc.Value.Kind == DateTimeKind.Utc ? sinal.FimUtc.Value : sinal.FimUtc.Value.ToUniversalTime()) : null;
        if (inicioUtc > DateTime.UtcNow.AddMinutes(10) || inicioUtc < DateTime.UtcNow.AddYears(-2)) return false;
        if (fimUtc.HasValue && fimUtc.Value < inicioUtc) return false;

        unidade = metrica.UnidadePadrao;
        return true;
    }

    private static string ChaveDeduplicacao(MetadataSinal x) =>
        !string.IsNullOrWhiteSpace(x.IdExterno)
            ? $"{x.Fonte}|{x.IdExterno}"
            : $"{x.Fonte}|{x.Metrica}|{x.InicioUtc:O}";

    private static string? LimparId(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var x = valor.Trim();
        return x[..Math.Min(160, x.Length)];
    }

    private static MetadataSinal? TentarLerMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;
            var fonte = raiz.TryGetProperty("Fonte", out var fonteEl) ? fonteEl.GetString() : null;
            var metrica = raiz.TryGetProperty("Metrica", out var metricaEl) ? metricaEl.GetString() : null;
            var idExterno = raiz.TryGetProperty("IdExterno", out var idEl) && idEl.ValueKind != JsonValueKind.Null ? idEl.GetString() : null;
            var qualidade = raiz.TryGetProperty("Qualidade", out var qualidadeEl) && qualidadeEl.ValueKind != JsonValueKind.Null ? qualidadeEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(fonte) || string.IsNullOrWhiteSpace(metrica)) return null;
            if (!raiz.TryGetProperty("InicioUtc", out var inicioEl) || !inicioEl.TryGetDateTime(out var inicioUtc)) return null;
            DateTime? fimUtc = null;
            if (raiz.TryGetProperty("FimUtc", out var fimEl) && fimEl.ValueKind != JsonValueKind.Null && fimEl.TryGetDateTime(out var fim)) fimUtc = fim;
            return new MetadataSinal(fonte, metrica, idExterno, inicioUtc, fimUtc, qualidade);
        }
        catch (JsonException) { return null; }
    }
}
