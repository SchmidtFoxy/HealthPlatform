using System.Text.Json;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Agua do paciente na tela Hoje: quanto bebeu, a meta do dia, os ultimos dias e a sequencia.
/// Com meta hidrica do profissional, o consumo fica no registro da meta (o mesmo que o profissional le).
/// Sem meta, cada copo vira um registro de Hidratacao no diario e o app mostra uma referencia
/// pelo peso (35 ml/kg), sem criar meta nenhuma. O lembrete fica em UsuariosTokens: sem SQL.
/// </summary>
public static class AguaPacienteService
{
    public const string Provedor = "AesynAgua";
    public const string NomeToken = "Lembrete";
    public const int MlPorKg = 35;
    public const int ReferenciaPadraoMl = 2000;
    private const int Dias = 14;

    public sealed record EstadoLembrete(bool Ativo, int FusoMinutos, string? UltimoEnvio);

    public sealed record DiaAgua(DateOnly Data, decimal Ml, bool Bateu);

    public sealed record ResumoAgua(
        decimal HojeMl,
        decimal MetaMl,
        string OrigemMeta,
        Guid? MetaId,
        decimal? PesoKg,
        int Sequencia,
        int MelhorSequencia,
        IReadOnlyList<DiaAgua> UltimosDias,
        bool LembreteAtivo);

    public static bool EhMetaDeAgua(MetaPaciente m)
    {
        var t = $"{m.Tipo} {m.Nome}".ToLowerInvariant();
        return t.Contains("hidrat") || t.Contains("agua") || t.Contains("água");
    }

    public static bool EmLitros(string? unidade) => (unidade ?? string.Empty).Trim().ToLowerInvariant() is "l" or "litro" or "litros";
    public static decimal ParaMl(decimal valor, string? unidade) => EmLitros(unidade) ? valor * 1000m : valor;
    public static decimal DeMl(decimal ml, string? unidade) => EmLitros(unidade) ? ml / 1000m : ml;

    /// <summary>Dia local do paciente para um horario UTC (fuso como o getTimezoneOffset do navegador).</summary>
    public static DateOnly DiaLocal(DateTime utc, int fusoMinutos) => DateOnly.FromDateTime(utc.AddMinutes(-fusoMinutos));

    public static int FusoValido(int? fuso) => Math.Clamp(fuso ?? 180, -840, 840);

    public static Task<MetaPaciente?> MetaAtivaAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct, bool rastrear = false)
    {
        var q = rastrear ? db.MetasPaciente : db.MetasPaciente.AsNoTracking();
        return q.Where(x => x.PacienteId == pacienteId && x.Status == "Ativa" && x.DataInicio <= dia &&
                            (!x.DataFim.HasValue || x.DataFim.Value >= dia) &&
                            (x.Tipo.ToLower().Contains("hidrat") || x.Nome.ToLower().Contains("agua") || x.Nome.ToLower().Contains("água") || x.Nome.ToLower().Contains("hidrat")))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<ResumoAgua> MontarAsync(AppDbContext db, Guid pacienteId, Guid usuarioId, DateOnly hoje, int fusoMinutos, CancellationToken ct)
    {
        var meta = await MetaAtivaAsync(db, pacienteId, hoje, ct);
        var inicio = hoje.AddDays(-(Dias - 1));
        var porDia = new Dictionary<DateOnly, decimal>();

        if (meta is not null)
        {
            var regs = await db.RegistrosMetas.AsNoTracking()
                .Where(x => x.MetaPacienteId == meta.Id && x.Data >= inicio && x.Data <= hoje && x.Valor != null)
                .Select(x => new { x.Data, x.Valor })
                .ToListAsync(ct);
            foreach (var r in regs) porDia[r.Data] = Math.Round(ParaMl(r.Valor!.Value, meta.Unidade), 0);
        }
        else
        {
            var deUtc = DateTime.SpecifyKind(inicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddMinutes(fusoMinutos);
            var ateUtc = DateTime.SpecifyKind(hoje.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddMinutes(fusoMinutos);
            var regs = await db.RegistrosDiarioPaciente.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.Tipo == "Hidratacao" && x.ValorNumerico != null &&
                            x.DataHoraUtc >= deUtc && x.DataHoraUtc < ateUtc)
                .Select(x => new { x.DataHoraUtc, x.ValorNumerico, x.Unidade })
                .ToListAsync(ct);
            foreach (var r in regs)
            {
                var dia = DiaLocal(r.DataHoraUtc, fusoMinutos);
                var ml = (r.Unidade ?? string.Empty).Trim().ToLowerInvariant() == "ml" ? r.ValorNumerico!.Value : r.ValorNumerico!.Value * 1000m;
                porDia[dia] = porDia.GetValueOrDefault(dia) + ml;
            }
        }

        decimal? peso = null;
        decimal metaMl;
        string origem;
        if (meta?.ValorObjetivo is > 0)
        {
            metaMl = Math.Round(ParaMl(meta.ValorObjetivo.Value, meta.Unidade), 0);
            origem = "Profissional";
        }
        else
        {
            peso = await db.Avaliacoes.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.PesoKg != null)
                .OrderByDescending(x => x.DataUtc)
                .Select(x => x.PesoKg)
                .FirstOrDefaultAsync(ct);
            if (peso is > 0)
            {
                // Arredonda de 50 em 50 ml e fica numa faixa razoavel para um adulto.
                metaMl = Math.Clamp(Math.Round(peso.Value * MlPorKg / 50m, 0) * 50m, 1500m, 4500m);
                origem = "Peso";
            }
            else
            {
                metaMl = ReferenciaPadraoMl;
                origem = "Padrao";
            }
        }

        var dias = Enumerable.Range(0, Dias)
            .Select(i => inicio.AddDays(i))
            .Select(d => new DiaAgua(d, Math.Max(0, porDia.GetValueOrDefault(d)), porDia.GetValueOrDefault(d) >= metaMl))
            .ToList();

        // Sequencia: dias seguidos batendo a meta ate hoje. Hoje ainda em aberto nao quebra a sequencia.
        var sequencia = 0;
        for (var i = dias.Count - 1; i >= 0; i--)
        {
            if (dias[i].Bateu) sequencia++;
            else if (i == dias.Count - 1) continue;
            else break;
        }
        var melhor = 0;
        var corrente = 0;
        foreach (var d in dias)
        {
            corrente = d.Bateu ? corrente + 1 : 0;
            melhor = Math.Max(melhor, corrente);
        }

        var lembrete = await LerLembreteAsync(db, usuarioId, ct);
        return new ResumoAgua(dias[^1].Ml, metaMl, origem, meta?.Id, peso, sequencia, melhor, dias, lembrete?.Ativo == true);
    }

    public static async Task<EstadoLembrete?> LerLembreteAsync(AppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var token = await db.UserTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == usuarioId && x.LoginProvider == Provedor && x.Name == NomeToken, ct);
        return Ler(token?.Value);
    }

    public static EstadoLembrete? Ler(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        try { return JsonSerializer.Deserialize<EstadoLembrete>(valor); }
        catch (JsonException) { return null; }
    }

    public static async Task SalvarLembreteAsync(AppDbContext db, Guid usuarioId, bool ativo, int fusoMinutos, CancellationToken ct)
    {
        var token = await db.UserTokens
            .FirstOrDefaultAsync(x => x.UserId == usuarioId && x.LoginProvider == Provedor && x.Name == NomeToken, ct);
        var atual = Ler(token?.Value);
        var valor = JsonSerializer.Serialize(new EstadoLembrete(ativo, fusoMinutos, atual?.UltimoEnvio));
        if (token is null)
            db.UserTokens.Add(new IdentityUserToken<Guid> { UserId = usuarioId, LoginProvider = Provedor, Name = NomeToken, Value = valor });
        else
            token.Value = valor;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Quanto o paciente ja deveria ter bebido a esta hora para fechar o dia na meta:
    /// distribui a meta das 7h as 21h (antes das 7h, zero; depois das 21h, a meta toda).
    /// </summary>
    public static decimal RitmoEsperadoMl(decimal metaMl, TimeOnly agoraLocal)
    {
        var horas = (decimal)(agoraLocal.ToTimeSpan().TotalHours - 7d);
        return Math.Round(metaMl * Math.Clamp(horas / 14m, 0m, 1m), 0);
    }
}
