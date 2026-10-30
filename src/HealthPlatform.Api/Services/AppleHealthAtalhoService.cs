using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.MonitoramentoPassivo;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Piloto Apple Health sem app nativo: o paciente instala um atalho do app Atalhos (iOS), que le o
/// resumo do dia no Saude e envia para a API com um token pessoal. O token fica so como hash em
/// UsuariosTokens (tabela do Identity que ja existe), entao nao ha mudanca de schema.
/// </summary>
public static partial class AppleHealthAtalhoService
{
    public const string Provedor = "AesynAppleHealth";
    public const string NomeToken = "Atalho";
    private const string Prefixo = "ah";
    private const string PrefixoIdExterno = "atalho:";

    private sealed record EstadoToken(string Hash, DateTime CriadoEmUtc, DateTime? UltimoUsoUtc);

    // Campo do JSON enviado pelo atalho -> metrica normalizada do monitoramento passivo.
    private static readonly (string Campo, string Metrica, decimal Fator)[] Campos =
    [
        ("passos", "Passos", 1m),
        ("energiaAtiva", "EnergiaAtiva", 1m),
        ("distanciaKm", "Distancia", 1m),
        ("minutosAtivos", "MinutosAtivos", 1m),
        ("sonoMinutos", "SonoMinutos", 1m),
        ("sonoHoras", "SonoMinutos", 60m),
        ("fcRepouso", "FrequenciaCardiacaRepouso", 1m),
        ("hrv", "HRV", 1m)
    ];

    public static async Task<string> GerarTokenAsync(AppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var segredo = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var token = $"{Prefixo}_{usuarioId:N}_{segredo}";
        var estado = new EstadoToken(Hash(token), DateTime.UtcNow, null);

        var atual = await BuscarRegistroAsync(db, usuarioId, ct);
        if (atual is null)
            db.UserTokens.Add(new IdentityUserToken<Guid> { UserId = usuarioId, LoginProvider = Provedor, Name = NomeToken, Value = JsonSerializer.Serialize(estado) });
        else
            atual.Value = JsonSerializer.Serialize(estado);
        await db.SaveChangesAsync(ct);
        return token;
    }

    public static async Task<bool> RevogarAsync(AppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var atual = await BuscarRegistroAsync(db, usuarioId, ct);
        if (atual is null) return false;
        db.UserTokens.Remove(atual);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<(bool Ativo, DateTime? CriadoEmUtc, DateTime? UltimoUsoUtc)> StatusAsync(AppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var atual = await BuscarRegistroAsync(db, usuarioId, ct);
        var estado = Ler(atual?.Value);
        return estado is null ? (false, null, null) : (true, estado.CriadoEmUtc, estado.UltimoUsoUtc);
    }

    /// <summary>Valida o token enviado pelo atalho e devolve o usuario dono, ja marcando o uso.</summary>
    public static async Task<Guid?> ValidarAsync(AppDbContext db, string? token, CancellationToken ct)
    {
        token = token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > 200) return null;
        var partes = token.Split('_', 3);
        if (partes.Length != 3 || partes[0] != Prefixo || !Guid.TryParseExact(partes[1], "N", out var usuarioId)) return null;

        var atual = await BuscarRegistroAsync(db, usuarioId, ct);
        var estado = Ler(atual?.Value);
        if (atual is null || estado is null) return null;

        var esperado = Encoding.ASCII.GetBytes(estado.Hash);
        var recebido = Encoding.ASCII.GetBytes(Hash(token));
        if (!CryptographicOperations.FixedTimeEquals(esperado, recebido)) return null;

        atual.Value = JsonSerializer.Serialize(estado with { UltimoUsoUtc = DateTime.UtcNow });
        return usuarioId;
    }

    /// <summary>
    /// Converte o resumo diario do atalho em sinais AppleHealth. Reenviar o mesmo dia substitui os
    /// valores anteriores desse dia, porque o atalho pode rodar mais de uma vez com dados parciais.
    /// </summary>
    public static async Task<AppleHealthAtalhoResultado> GravarResumoDiarioAsync(AppDbContext db, Guid pacienteId, JsonElement corpo, CancellationToken ct)
    {
        if (corpo.ValueKind != JsonValueKind.Object)
            return AppleHealthAtalhoResultado.Erro("Envie um dicionario com os valores do dia.");

        var hoje = DateTime.UtcNow.AddHours(-3).Date;
        var dia = hoje.AddDays(-1);
        var textoData = Texto(Campo(corpo, "data"));
        if (!string.IsNullOrWhiteSpace(textoData))
        {
            if (!TentarLerData(textoData, out dia))
                return AppleHealthAtalhoResultado.Erro($"Data invalida: \"{textoData}\". Use o formato aaaa-MM-dd.");
            if (dia > hoje || dia < hoje.AddDays(-30))
                return AppleHealthAtalhoResultado.Erro("A data precisa estar entre hoje e 30 dias atras.");
        }

        var valores = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (campo, metrica, fator) in Campos)
        {
            var el = Campo(corpo, campo);
            if (el is null || valores.ContainsKey(metrica)) continue;
            if (TentarLerNumero(el.Value, out var numero) && numero >= 0)
            {
                // Passos sao inteiros: "8.234" vindo como texto num iPhone em portugues e milhar.
                if (metrica == "Passos" && el.Value.ValueKind == JsonValueKind.String && MilharRegex().IsMatch(el.Value.GetString()!.Trim()))
                    numero = decimal.Parse(Regex.Replace(el.Value.GetString()!, @"[^\d]", string.Empty), CultureInfo.InvariantCulture);
                valores[metrica] = Math.Round(numero * fator, 1);
            }
        }

        if (valores.Count == 0)
            return AppleHealthAtalhoResultado.Erro("Nenhum valor reconhecido. Campos aceitos: " + string.Join(", ", Campos.Select(x => x.Campo)) + ".");

        // Meio-dia UTC cai no mesmo dia em todo o Brasil, entao o registro aparece na data certa.
        var inicioUtc = DateTime.SpecifyKind(dia.AddHours(12), DateTimeKind.Utc);
        var idsDoDia = valores.Keys.Select(m => IdExterno(dia, m)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var anteriores = await db.RegistrosDiarioPaciente
            .Where(x => x.PacienteId == pacienteId && x.Tipo == MonitoramentoPassivoService.TipoRegistro && x.DataHoraUtc == inicioUtc)
            .ToListAsync(ct);
        db.RegistrosDiarioPaciente.RemoveRange(anteriores.Where(x =>
            x.Descricao is not null && idsDoDia.Any(id => x.Descricao.Contains($"\"{id}\"", StringComparison.OrdinalIgnoreCase))));
        await db.SaveChangesAsync(ct);

        var sinais = valores.Select(par => new MonitoramentoPassivoSinalRequest(
            "AppleHealth", par.Key, inicioUtc, null, par.Value, null, IdExterno(dia, par.Key), "Atalho iOS")).ToList();
        var importacao = await MonitoramentoPassivoService.ImportarAsync(db, pacienteId, new MonitoramentoPassivoImportacaoRequest(sinais), ct);

        var rotulos = valores.Select(par => $"{Rotulo(par.Key)}: {par.Value.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR"))}");
        return new AppleHealthAtalhoResultado(true, dia, importacao.Importados,
            $"Aesyn recebeu {dia:dd/MM}: " + string.Join(" · ", rotulos));
    }

    private static string IdExterno(DateTime dia, string metrica) => $"{PrefixoIdExterno}{dia:yyyy-MM-dd}:{metrica}";

    private static string Rotulo(string metrica) => metrica switch
    {
        "Passos" => "passos",
        "EnergiaAtiva" => "energia ativa (kcal)",
        "Distancia" => "distancia (km)",
        "MinutosAtivos" => "minutos de exercicio",
        "SonoMinutos" => "sono (min)",
        "FrequenciaCardiacaRepouso" => "FC repouso (bpm)",
        "HRV" => "HRV (ms)",
        _ => metrica
    };

    private static Task<IdentityUserToken<Guid>?> BuscarRegistroAsync(AppDbContext db, Guid usuarioId, CancellationToken ct) =>
        db.UserTokens.FirstOrDefaultAsync(x => x.UserId == usuarioId && x.LoginProvider == Provedor && x.Name == NomeToken, ct);

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static EstadoToken? Ler(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<EstadoToken>(json); }
        catch (JsonException) { return null; }
    }

    private static JsonElement? Campo(JsonElement corpo, string nome)
    {
        foreach (var p in corpo.EnumerateObject())
            if (string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return null;
    }

    private static string? Texto(JsonElement? el) => el?.ValueKind switch
    {
        JsonValueKind.String => el.Value.GetString(),
        JsonValueKind.Number => el.Value.GetRawText(),
        _ => null
    };

    private static bool TentarLerData(string texto, out DateTime dia)
    {
        texto = texto.Trim();
        string[] formatos = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:sszzz"];
        if (DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out dia))
        {
            dia = dia.Date;
            return true;
        }
        if (texto.Length >= 10 && DateTime.TryParseExact(texto[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dia))
            return true;
        dia = default;
        return false;
    }

    /// <summary>
    /// O Atalhos pode mandar numero JSON ou texto ("8.234", "5,3", "58 bpm"), dependendo de como o
    /// campo foi montado e do idioma do iPhone. Aceita os dois.
    /// </summary>
    internal static bool TentarLerNumero(JsonElement el, out decimal valor)
    {
        valor = 0;
        if (el.ValueKind == JsonValueKind.Number) return el.TryGetDecimal(out valor);
        if (el.ValueKind != JsonValueKind.String) return false;
        return TentarLerNumero(el.GetString(), out valor);
    }

    internal static bool TentarLerNumero(string? texto, out decimal valor)
    {
        valor = 0;
        var m = NumeroRegex().Match(texto ?? string.Empty);
        if (!m.Success) return false;
        var s = m.Value;
        var ultimoPonto = s.LastIndexOf('.');
        var ultimaVirgula = s.LastIndexOf(',');
        if (ultimoPonto >= 0 && ultimaVirgula >= 0)
        {
            // O separador que aparece por ultimo e o decimal; o outro e de milhar.
            var milhar = ultimoPonto > ultimaVirgula ? ',' : '.';
            s = s.Replace(milhar.ToString(), string.Empty).Replace(',', '.');
        }
        else if (ultimaVirgula >= 0)
        {
            s = s.Count(c => c == ',') > 1 ? s.Replace(",", string.Empty) : s.Replace(',', '.');
        }
        else if (s.Count(c => c == '.') > 1)
        {
            s = s.Replace(".", string.Empty);
        }
        return decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);
    }

    [GeneratedRegex(@"\d[\d.,]*")]
    private static partial Regex NumeroRegex();

    [GeneratedRegex(@"^\d{1,3}([.,]\d{3})+$")]
    private static partial Regex MilharRegex();
}

public sealed record AppleHealthAtalhoResultado(bool Ok, DateTime? Dia, int Importados, string Mensagem)
{
    public static AppleHealthAtalhoResultado Erro(string mensagem) => new(false, null, 0, mensagem);
}
