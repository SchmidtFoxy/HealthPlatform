using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class GamificacaoService
{
    public static async Task RegistrarEventoAsync(AppDbContext db, Guid organizacaoId, Guid pacienteId,
        DateOnly data, string fonte, Guid fonteId, int pontos, string motivo, string? adequacao, CancellationToken ct)
    {
        if (pontos <= 0) return;
        var existe = await db.EventosXp.AnyAsync(x => x.PacienteId == pacienteId && x.Fonte == fonte && x.FonteId == fonteId, ct);
        if (existe) return;
        db.EventosXp.Add(new EventoXp
        {
            OrganizacaoId = organizacaoId, PacienteId = pacienteId, Data = data, Fonte = fonte,
            FonteId = fonteId, Pontos = pontos, Motivo = motivo, Adequacao = adequacao
        });
    }

    public static (int Pontos, string Adequacao, string Motivo) CalcularXpTreino(string? recomendacao, int? esforco)
    {
        if (!esforco.HasValue) return (70, "SemRpe", "Treino concluido; informe o esforco percebido para personalizar o XP.");
        static int NivelEsforco(int rpe) => rpe <= 3 ? 0 : rpe <= 5 ? 1 : rpe <= 8 ? 2 : 3;
        static int NivelRecomendacao(string? r) => r switch { "Recuperacao" => 0, "Leve" => 1, "Pesado" => 3, _ => 2 };
        if (string.IsNullOrWhiteSpace(recomendacao)) return (70, "SemCheckIn", "Treino concluido sem prontidao registrada; XP neutro.");
        var delta = NivelEsforco(esforco.Value) - NivelRecomendacao(recomendacao);
        if (delta == 0) return (100, "Ideal", "Treino alinhado a prontidao do dia.");
        if (Math.Abs(delta) == 1) return (75, delta > 0 ? "Acima" : "Abaixo", "Treino proximo da intensidade sugerida.");
        if (delta >= 2) return (35, "Excesso", "Treino muito acima da intensidade sugerida; XP reduzido para nao premiar sobrecarga.");
        return (60, "Conservador", "Treino abaixo da intensidade sugerida, preservando consistencia sem punicao severa.");
    }

    public static async Task<PortalGamificacaoResponse> MontarResumoAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var eventos = await db.EventosXp.AsNoTracking().Where(x => x.PacienteId == pacienteId).ToListAsync(ct);
        var totalXp = eventos.Sum(x => x.Pontos);
        var nivel = totalXp / 500 + 1;
        var xpNivel = totalXp % 500;
        var desde = dia.AddDays(-13);
        var diasAtivos = eventos.Where(x => x.Data >= desde && x.Data <= dia).Select(x => x.Data).Distinct().ToHashSet();
        var consistencia = Math.Min(100, diasAtivos.Count * 10); // 10 dias ativos em 14 = 100; frequencia sustentavel, nao perfeicao.
        var referencia = dia;
        if (!diasAtivos.Contains(referencia) && diasAtivos.Contains(dia.AddDays(-1))) referencia = dia.AddDays(-1);
        var streak = 0;
        while (diasAtivos.Contains(referencia.AddDays(-streak))) streak++;
        var hojeXp = eventos.Where(x => x.Data == dia).Sum(x => x.Pontos);
        var recentes = eventos.OrderByDescending(x => x.CreatedAtUtc).Take(5)
            .Select(x => new PortalEventoXpResponse(x.Id, x.Data, x.Fonte, x.Pontos, x.Motivo, x.Adequacao)).ToList();
        return new PortalGamificacaoResponse(totalXp, nivel, xpNivel, 500, consistencia, streak, hojeXp, diasAtivos.Count, recentes);
    }
}
