using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class EstrategiaDiariaService
{
    public static async Task<PortalEstrategiaDiaResponse> MontarAsync(AppDbContext db, Guid pacienteId, DateOnly dia, PortalProntidaoDiariaResponse? prontidao, CancellationToken ct)
    {
        var ciclo = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia && x.DataFim >= dia)
            .OrderByDescending(x => x.DataInicio).FirstOrDefaultAsync(ct);

        var faseTreinoId = ciclo?.FaseTreinoId;
        var faseNutricionalId = ciclo?.FaseNutricionalId;
        Guid? faseTreinoPlanoId = null;
        Guid? faseNutricionalPlanoId = null;
        if (faseTreinoId.HasValue)
            faseTreinoPlanoId = await db.FasesTreino.AsNoTracking().Where(x => x.Id == faseTreinoId.Value).Select(x => x.PlanoTreinoId).FirstOrDefaultAsync(ct);
        if (faseNutricionalId.HasValue)
            faseNutricionalPlanoId = await db.FasesNutricionais.AsNoTracking().Where(x => x.Id == faseNutricionalId.Value).Select(x => x.PlanoAlimentarId).FirstOrDefaultAsync(ct);

        var planoTreino = await db.PlanosTreino.AsNoTracking().Include(x => x.Sessoes)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia && (!x.DataFim.HasValue || x.DataFim.Value >= dia) && (!faseTreinoPlanoId.HasValue || x.Id == faseTreinoPlanoId.Value))
            .OrderByDescending(x => x.DataInicio).FirstOrDefaultAsync(ct);
        var planoAlimentar = await db.PlanosAlimentares.AsNoTracking().Include(x => x.Refeicoes)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia && (!x.DataFim.HasValue || x.DataFim.Value >= dia) && (!faseNutricionalPlanoId.HasValue || x.Id == faseNutricionalPlanoId.Value))
            .OrderByDescending(x => x.DataInicio).FirstOrDefaultAsync(ct);

        var intensidade = prontidao?.RecomendacaoTreino ?? "SemCheckIn";
        var perfil = intensidade switch
        {
            "Recuperacao" => "Recuperacao",
            "Leve" => "CargaControlada",
            "Normal" => "TreinoPlanejado",
            "Pesado" => "AltaProntidao",
            _ => "AguardandoCheckIn"
        };
        var (rpeMin, rpeMax, ajusteCarga, ajusteVolume, treino) = intensidade switch
        {
            "Recuperacao" => (2, 4, -25, -30, "Priorize recuperação ativa, mobilidade ou descanso. Se houver sessão prescrita, reduza a exigência e preserve técnica."),
            "Leve" => (4, 6, -15, -20, "Execute o que estiver prescrito com carga e volume controlados; pare antes da falha e preserve a qualidade do movimento."),
            "Normal" => (6, 8, 0, 0, "Cumpra a sessão planejada dentro da prescrição. Use o RPE como limite e não acrescente volume por conta própria."),
            "Pesado" => (8, 9, 0, 0, "Boa prontidão para uma sessão exigente somente se ela já estiver prevista. Não aumente carga ou volume automaticamente acima da prescrição."),
            _ => (5, 7, 0, 0, "Faça o check-in antes de decidir a intensidade. Até lá, siga a prescrição sem progressões adicionais.")
        };

        var nutricao = intensidade switch
        {
            "Recuperacao" => "Mantenha o plano alimentar definido. Distribua as refeições normalmente e priorize recuperação; não faça cortes extras de energia por conta própria.",
            "Leve" => "Mantenha o plano. Se treinar, use as refeições previstas próximas ao treino e evite compensações fora da prescrição.",
            "Normal" => "Siga o plano alimentar e preserve as refeições pré/pós-treino que já estiverem previstas pelo profissional.",
            "Pesado" => "Siga integralmente o plano do ciclo; não reduza carboidratos ou refeições prescritas em um dia de maior demanda.",
            _ => "Siga o plano alimentar atual sem alterações até registrar a prontidão do dia."
        };
        var hidratacao = intensidade switch
        {
            "Pesado" => "Antecipe a hidratação ao longo do dia e acompanhe a meta definida; maior esforço não autoriza exceder orientações clínicas específicas.",
            "Recuperacao" => "Hidrate-se de forma regular ao longo do dia e use a recuperação como prioridade.",
            _ => "Distribua a hidratação ao longo do dia e acompanhe a meta já definida no seu plano."
        };

        var sessoesPlano = planoTreino?.Sessoes.OrderBy(x => x.Ordem).ToList() ?? new List<HealthPlatform.Domain.Entities.SessaoTreino>();
        var tokensDia = dia.DayOfWeek switch
        {
            DayOfWeek.Monday => new[] { "segunda", "seg", "monday", "mon" },
            DayOfWeek.Tuesday => new[] { "terça", "terca", "ter", "tuesday", "tue" },
            DayOfWeek.Wednesday => new[] { "quarta", "qua", "wednesday", "wed" },
            DayOfWeek.Thursday => new[] { "quinta", "qui", "thursday", "thu" },
            DayOfWeek.Friday => new[] { "sexta", "sex", "friday", "fri" },
            DayOfWeek.Saturday => new[] { "sábado", "sabado", "sáb", "sab", "saturday", "sat" },
            _ => new[] { "domingo", "dom", "sunday", "sun" }
        };
        var sessoesHoje = sessoesPlano.Where(x => string.IsNullOrWhiteSpace(x.DiasSemana) || tokensDia.Any(t => x.DiasSemana!.Contains(t, StringComparison.OrdinalIgnoreCase))).ToList();
        var sessoes = (sessoesHoje.Count > 0 ? sessoesHoje : sessoesPlano).Select(x => x.Nome).Take(3).ToList();
        var refeicoes = planoAlimentar?.Refeicoes.OrderBy(x => x.Ordem).Select(x => x.Nome).Take(4).ToList() ?? new List<string>();
        var contexto = ciclo is null ? "sem ciclo esportivo ativo" : $"ciclo {ciclo.Nome} ({ciclo.PerfilEsportivo})";
        var justificativa = prontidao is null
            ? $"Estratégia conservadora: prontidão ainda não registrada; {contexto}."
            : $"Prontidão {prontidao.Score}/100 ({prontidao.RecomendacaoTreino}) dentro do {contexto}. A adaptação nunca aumenta automaticamente a prescrição.";

        return new PortalEstrategiaDiaResponse(perfil, intensidade, rpeMin, rpeMax, ajusteCarga, ajusteVolume, treino, nutricao, hidratacao, justificativa, sessoes, refeicoes);
    }
}
