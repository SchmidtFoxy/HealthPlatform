using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class RetornoGradualService
{
    public static async Task<PortalRetornoGradualResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia,
        PortalMapaCorporalLongitudinalResponse mapaCorporal,
        PortalReadinessContextualTreinoResponse readiness,
        PortalRespostaSessaoResponse respostaSessao,
        PortalCargaIndividualizadaResponse cargaIndividualizada,
        CancellationToken ct)
    {
        // Retorno gradual precisa estar ancorado em sessoes realmente registradas; ausencia de treino nao vira "retorno de lesao" por inferencia.
        // A referencia usa ate 42 dias para detectar uma pausa observavel entre sessoes concluidas do proprio atleta.
        const int janelaDias = 42;
        const int pausaMinimaDias = 7;
        var fimUtc = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var inicioUtc = fimUtc.AddDays(-janelaDias);

        var sessoes = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .Select(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var diasTreino = sessoes.Select(DateOnly.FromDateTime).Distinct().OrderByDescending(x => x).ToList();
        var ultimaSessao = sessoes.FirstOrDefault();
        DateTime? ultimaSessaoUtc = sessoes.Count > 0 ? ultimaSessao : null;
        DateTime? sessaoRetornoUtc = null;
        int? diasPausa = null;
        var pausaDetectada = false;
        var retornoEmCurso = false;

        for (var i = 0; i < diasTreino.Count - 1; i++)
        {
            var maisRecente = diasTreino[i];
            var anterior = diasTreino[i + 1];
            var intervalo = maisRecente.DayNumber - anterior.DayNumber - 1;
            if (intervalo >= pausaMinimaDias)
            {
                pausaDetectada = true;
                diasPausa = intervalo;
                var retornoDate = sessoes.Where(x => DateOnly.FromDateTime(x) == maisRecente).OrderBy(x => x).First();
                sessaoRetornoUtc = retornoDate;
                retornoEmCurso = maisRecente >= dia.AddDays(-7);
                break;
            }
        }

        // Se houve atividade anterior, mas nenhum treino nos ultimos 7 dias, ha uma pausa atual observavel em preparacao para retorno.
        var diasDesdeUltimoTreino = diasTreino.Count > 0 ? dia.DayNumber - diasTreino[0].DayNumber : (int?)null;
        var pausaAtual = diasDesdeUltimoTreino.HasValue && diasDesdeUltimoTreino.Value >= pausaMinimaDias && diasDesdeUltimoTreino.Value <= 28;
        if (pausaAtual && !pausaDetectada)
        {
            pausaDetectada = true;
            diasPausa = diasDesdeUltimoTreino;
        }

        var corteDor = dia.AddDays(-7);
        var regioesDorRecente = mapaCorporal.Regioes
            .Where(x => x.UltimoRegistro >= corteDor && (x.Estado == "Acompanhar" || x.IntensidadeMaxima >= 5 || x.ImpactoMaximoTreino >= 5))
            .ToList();
        var dorRecente = regioesDorRecente.Count > 0;

        var contexto = pausaDetectada && dorRecente ? "PausaEDor" : pausaDetectada ? "Pausa" : dorRecente ? "Dor" : "SemContextoDeRetorno";
        var criterios = new List<PortalRetornoGradualCriterioResponse>
        {
            new("historico-sessoes", pausaDetectada ? "PausaDetectada" : "SemPausaRelevante", "Historico real de sessoes",
                pausaDetectada ? $"Pausa observada de {diasPausa} dia(s) sem sessao concluida." : "Nao foi encontrada pausa de 7 dias ou mais que caracterize retorno nesta janela."),
            new("dor", dorRecente ? "Presente" : "SemSinalRelevante", "Dor recente",
                dorRecente ? $"{regioesDorRecente.Count} regiao(oes) com registro recente de intensidade/impacto relevante." : "Nao ha registro corporal recente que, isoladamente, caracterize contexto de retorno por dor."),
            new("readiness", readiness.Estado, "Readiness contextual", readiness.Resumo),
            new("resposta", respostaSessao.Estado, "Resposta a ultima sessao", respostaSessao.Resumo),
            new("carga", cargaIndividualizada.Estado, "Carga individualizada", cargaIndividualizada.Resumo)
        };

        const string seguranca = "Retorno gradual e uma organizacao observacional: nao e protocolo medico, nao diagnostica lesao, nao libera retorno esportivo, nao define prazo biologico e nao aumenta carga automaticamente. Dor persistente/importante ou limitacao funcional requer avaliacao profissional apropriada.";

        if (sessoes.Count == 0 && mapaCorporal.TotalRegistros == 0)
            return new("DadosInsuficientes", "Retorno gradual apos pausa/dor",
                "Ainda nao existe historico suficiente de sessoes ou dor localizada para caracterizar um retorno gradual.", contexto, null, null, null, false, false,
                readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
                "Construa historico com os registros habituais; nao aumente exigencia apenas para gerar dados.", seguranca);

        if (!pausaDetectada && !dorRecente)
            return new("SemRetornoAtivo", "Retorno gradual apos pausa/dor",
                "Nao ha pausa relevante ou sinal corporal recente que justifique abrir uma trilha de retorno neste momento.", contexto, null, ultimaSessaoUtc, null, false, false,
                readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
                "Mantenha o planejamento habitual e continue observando resposta, recuperacao e dor sem criar restricao artificial.", seguranca);

        var exigeRevisao = dorRecente && (readiness.Estado is "RevisarAntesDaSessao" or "AdaptarAoContexto" ||
                           respostaSessao.Estado == "Revisar" || cargaIndividualizada.Estado == "RevisarContexto");
        if (exigeRevisao)
            return new("RevisarAntesRetorno", "Retorno gradual apos pausa/dor",
                "Ha contexto de pausa/dor acompanhado de sinais que pedem revisao antes de avancar a demanda esportiva.", contexto, diasPausa, ultimaSessaoUtc, sessaoRetornoUtc, dorRecente, false,
                readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
                "Revise sintomas, funcao e sessao com o profissional antes de avancar. Um passo por vez; nao compense o periodo de pausa.", seguranca);

        if (pausaAtual && !retornoEmCurso)
            return new("RetornoEmPreparacao", "Retorno gradual apos pausa/dor",
                "Existe uma pausa atual observavel; o retorno deve preservar a demanda ja supervisionada e considerar o estado do dia.", contexto, diasPausa, ultimaSessaoUtc, null, dorRecente, false,
                readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
                "Planeje a primeira exposicao com supervisao existente e observe a resposta antes de qualquer progressao seguinte. Um passo por vez.", seguranca);

        if (!pausaDetectada && dorRecente)
            return new("RetornoEmPreparacao", "Retorno gradual apos pausa/dor",
                "Ha dor recente registrada, mas nao ha uma pausa esportiva documentada que permita afirmar que o atleta ja esta em retorno apos interrupcao.", contexto, null, ultimaSessaoUtc, null, true, false,
                readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
                "Use dor, funcao, readiness e resposta como contexto para decidir a proxima exposicao com supervisao; um passo por vez, sem presumir lesao ou acelerar carga.", seguranca);

        var podeAvancar = retornoEmCurso && respostaSessao.Estado == "RespostaEstavel" && readiness.Estado == "CompativelComSessao" && !dorRecente;

        return new("RetornoEmCurso", "Retorno gradual apos pausa/dor",
            "O historico mostra retorno recente apos pausa e a proxima decisao deve depender da resposta observada, nao da vontade de recuperar o tempo parado.", contexto, diasPausa, ultimaSessaoUtc, sessaoRetornoUtc, dorRecente, podeAvancar,
            readiness.Estado, respostaSessao.Estado, cargaIndividualizada.Estado, criterios,
            podeAvancar
                ? "Ha criterios observacionais favoraveis para discutir a proxima etapa supervisionada; mantenha um passo por vez e nao aumente carga automaticamente."
                : "Mantenha a etapa atual ate existir resposta suficiente e compativel. Nao compense sessoes perdidas nem acelere por motivacao isolada.", seguranca);
    }
}
