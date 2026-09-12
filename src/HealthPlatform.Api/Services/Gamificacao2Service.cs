using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class Gamificacao2Service
{
    public static PortalGamificacao2Response Montar(
        PortalGamificacaoResponse gamificacao,
        PortalReadinessContextualTreinoResponse readiness,
        PortalDeloadRecuperacaoPlanejadaResponse deload,
        PortalRespostaSessaoResponse respostaSessao,
        PortalRetornoGradualResponse retornoGradual)
    {
        // Gamificacao 2.0 recompensa adequacao, consistencia e recuperacao coerente; nao sofrimento nem intensidade bruta.
        // XP alto nao significa "treine mais" e streak nao deve ser usado isoladamente para cobrar atividade em dia de recuperacao.
        // descanso planejado e adaptacao coerente fazem parte do progresso esportivo.
        var recentes = gamificacao.EventosRecentes ?? Array.Empty<PortalEventoXpResponse>();
        var alinhados = recentes.Count(x => x.Adequacao is "Ideal" or "Consistencia" or "Marco" or "Conservador");
        var excessos = recentes.Count(x => x.Adequacao == "Excesso");
        var sinais = new List<PortalGamificacao2SinalResponse>();

        sinais.Add(new(
            "adequacao-xp", "XP", excessos > 0 ? "Observar" : "Coerente", "Qualidade do XP",
            excessos > 0
                ? $"{excessos} evento(s) recente(s) foram marcados como excesso; intensidade acima da estrategia nao recebe recompensa maior."
                : "Os eventos recentes nao mostram recompensa extra por excesso de intensidade."));

        sinais.Add(new(
            "consistencia", "Rotina", gamificacao.ConsistenciaScore >= 80 ? "Estavel" : gamificacao.ConsistenciaScore >= 60 ? "Consolidando" : "Construir",
            "Consistencia sustentavel",
            $"Consistencia atual {gamificacao.ConsistenciaScore}/100 em {gamificacao.DiasAtivos14}/14 dias ativos; continuidade vale mais que um pico isolado."));

        if (deload.IntencaoPlanejada)
            sinais.Add(new("recuperacao-planejada", "Recuperacao", "ContaComoProgresso", "Recuperacao planejada faz parte do ciclo",
                $"{deload.TipoIntencao}: reduzir exigencia por estrategia nao deve gerar cobranca para compensar depois."));

        if (retornoGradual.Estado is "RetornoEmPreparacao" or "RetornoEmCurso" or "RevisarAntesRetorno")
            sinais.Add(new("retorno-gradual", "Retorno", retornoGradual.Estado, "Retorno sustentavel",
                "No retorno, cumprir o passo atual com boa resposta vale mais do que recuperar rapidamente volume perdido."));

        if (respostaSessao.Estado is "Revisar" or "Observar")
            sinais.Add(new("resposta-sessao", "Resposta", respostaSessao.Estado, "Resposta do corpo antes de nova cobranca",
                "A resposta posterior a sessao pede observacao; gamificacao nao deve pressionar uma nova progressao para manter XP ou streak."));

        if (readiness.Estado is "RecuperacaoPlanejada" or "AdaptarAoContexto" or "RevisarAntesDaSessao")
            sinais.Add(new("readiness-contextual", "Readiness", readiness.Estado, "Adequacao ao dia",
                "Seguir a adaptacao indicada pelo contexto e comportamento aderente; nao ha bonus por ultrapassar a demanda planejada."));

        string estado, foco, mensagem;
        if (deload.IntencaoPlanejada)
        {
            estado = "RecuperacaoConta"; foco = "Recuperacao planejada";
            mensagem = "O objetivo agora e respeitar a estrategia de recuperacao. Manter o plano vale mais que acumular intensidade.";
        }
        else if (retornoGradual.Estado is "RetornoEmPreparacao" or "RetornoEmCurso" or "RevisarAntesRetorno")
        {
            estado = "RetornoSustentavel"; foco = "Retorno gradual";
            mensagem = "O progresso desta fase e repetir passos toleraveis e observar a resposta; nao compense o periodo de pausa.";
        }
        else if (respostaSessao.Estado == "Revisar" || readiness.Estado == "RevisarAntesDaSessao")
        {
            estado = "PriorizarResposta"; foco = "Recuperacao e resposta";
            mensagem = "Antes de buscar mais XP ou intensidade, preserve a resposta ao treino e revise o contexto.";
        }
        else if (gamificacao.ConsistenciaScore < 60)
        {
            estado = "ConstruirConsistencia"; foco = "Rotina repetivel";
            mensagem = "Poucas acoes repetiveis valem mais que tentar recuperar tudo de uma vez.";
        }
        else
        {
            estado = "ConsistenciaSustentavel"; foco = "Manter o que funciona";
            mensagem = "Continue aderente ao plano e a prontidao. Nao existe bonus por fazer mais do que o contexto pede.";
        }

        const string seguranca = "Gamificacao 2.0 nao cria XP negativo, nao pune descanso planejado, nao recompensa sofrimento, nao usa streak como obrigacao e nao autoriza aumento automatico de carga, volume ou intensidade.";
        return new(estado, "Gamificacao 2.0", foco, mensagem, gamificacao.XpTotal, gamificacao.Nivel,
            gamificacao.ConsistenciaScore, gamificacao.StreakDias, alinhados, excessos, sinais, seguranca);
    }
}
