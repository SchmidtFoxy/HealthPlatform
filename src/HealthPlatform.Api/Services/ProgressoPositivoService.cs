using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class ProgressoPositivoService
{
    public static async Task<PortalProgressoPositivoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, PortalGamificacaoResponse gamificacao, CancellationToken ct)
    {
        var inicioSemana = dia.AddDays(-6);
        var inicioAnterior = dia.AddDays(-13);
        var fimAnterior = dia.AddDays(-7);

        var eventos = await db.EventosXp.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicioAnterior && x.Data <= dia)
            .Select(x => new { x.Data, x.Pontos, x.Fonte, x.Adequacao })
            .ToListAsync(ct);

        var xpSemana = eventos.Where(x => x.Data >= inicioSemana).Sum(x => Math.Max(0, x.Pontos));
        var xpSemanaAnterior = eventos.Where(x => x.Data >= inicioAnterior && x.Data <= fimAnterior).Sum(x => Math.Max(0, x.Pontos));
        var diasComProgresso7 = eventos.Where(x => x.Data >= inicioSemana && x.Pontos > 0).Select(x => x.Data).Distinct().Count();
        var fontes = eventos.Where(x => x.Data >= inicioSemana && x.Pontos > 0)
            .GroupBy(x => x.Fonte)
            .OrderByDescending(g => g.Sum(x => x.Pontos))
            .Take(3)
            .Select(g => new PortalProgressoPositivoDestaqueResponse(
                g.Key,
                g.Sum(x => Math.Max(0, x.Pontos)),
                g.Select(x => x.Data).Distinct().Count()))
            .ToList();

        var xpNoNivel = Math.Max(0, gamificacao.XpNoNivel);
        var xpProximoNivel = Math.Max(1, gamificacao.XpProximoNivel);
        var progressoNivel = Math.Round(Math.Clamp(xpNoNivel / (decimal)xpProximoNivel * 100m, 0m, 100m), 1);
        var faltamXp = Math.Max(0, xpProximoNivel - xpNoNivel);

        var estado = gamificacao.ConsistenciaScore >= 80 ? "RitmoSustentavel"
            : gamificacao.ConsistenciaScore >= 60 ? "ConstruindoConsistencia"
            : "RetomadaPositiva";

        var resumo = diasComProgresso7 switch
        {
            >= 5 => $"Você somou {xpSemana} XP em {diasComProgresso7} dias desta semana. O foco é repetir o que funciona, sem precisar compensar dias mais leves.",
            >= 2 => $"Você já somou {xpSemana} XP em {diasComProgresso7} dias. Cada ação coerente mantém o progresso avançando.",
            1 => $"Você já abriu a semana com {xpSemana} XP. O próximo passo é apenas manter uma rotina possível.",
            _ => "Seu XP acumulado continua preservado. Retome com uma ação possível quando fizer sentido para o seu plano."
        };

        var tendencia = xpSemanaAnterior <= 0
            ? (xpSemana > 0 ? "SemanaEmMovimento" : "SemComparacao")
            : xpSemana >= xpSemanaAnterior ? "RitmoMantidoOuMaior" : "RitmoMaisLeve";

        const string mensagemSeguranca = "AESYN XP é cumulativo: não existe XP negativo. Descanso planejado, recuperação e uma semana mais leve não apagam o progresso já conquistado e não devem ser compensados com excesso de treino ou restrição alimentar.";

        return new PortalProgressoPositivoResponse(
            estado, tendencia, gamificacao.XpTotal, gamificacao.Nivel, xpSemana, xpSemanaAnterior,
            diasComProgresso7, gamificacao.StreakDias, gamificacao.ConsistenciaScore, progressoNivel, faltamXp,
            resumo, fontes, mensagemSeguranca);
    }
}
