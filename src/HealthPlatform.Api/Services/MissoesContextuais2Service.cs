using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class MissoesContextuais2Service
{
    public static PortalMissoesContextuais2Response Montar(
        PortalGamificacaoResponse gamificacao,
        PortalGamificacao2Response gamificacao2,
        PortalReadinessContextualTreinoResponse readiness,
        PortalDeloadRecuperacaoPlanejadaResponse deload,
        PortalRespostaSessaoResponse respostaSessao,
        PortalRetornoGradualResponse retornoGradual)
    {
        // Missoes Contextuais 2.0 preservam progresso real sem transformar desafio em cobranca cega.
        // Prontidao, recuperacao planejada, retorno gradual e resposta corporal prevalecem sobre urgencia de XP/streak.
        var protegerHoje = deload.IntencaoPlanejada
            || retornoGradual.Estado is "RetornoEmPreparacao" or "RetornoEmCurso" or "RevisarAntesRetorno"
            || readiness.Estado is "RecuperacaoPlanejada" or "RevisarAntesDaSessao"
            || respostaSessao.Estado == "Revisar";

        string estado, foco, mensagem;
        if (deload.IntencaoPlanejada)
        {
            estado = "RecuperacaoProtegida";
            foco = "Respeitar a recuperacao planejada";
            mensagem = "As missoes continuam registrando o progresso da semana, mas nao criam urgencia para treinar em um dia de recuperacao planejada.";
        }
        else if (retornoGradual.Estado is "RetornoEmPreparacao" or "RetornoEmCurso" or "RevisarAntesRetorno")
        {
            estado = "RetornoProtegido";
            foco = "Cumprir o passo atual do retorno";
            mensagem = "No retorno gradual, completar menos com boa resposta pode ser mais adequado do que perseguir a meta semanal.";
        }
        else if (readiness.Estado == "RevisarAntesDaSessao" || respostaSessao.Estado == "Revisar")
        {
            estado = "RespostaPrimeiro";
            foco = "Revisar contexto antes da missao";
            mensagem = "A resposta do corpo vem antes do desafio. XP, streak e meta semanal nao justificam ignorar sinais de revisao.";
        }
        else
        {
            estado = "MissoesAtivas";
            foco = gamificacao2.FocoAtual;
            mensagem = "As missoes apoiam consistencia e adequacao. Elas nao exigem intensidade extra nem compensacao para fechar a semana.";
        }

        var itens = (gamificacao.DesafiosSemana ?? Array.Empty<PortalDesafioSemanalResponse>())
            .Select(x => new PortalMissaoContextual2ItemResponse(
                x.Id, x.Codigo, x.Titulo, x.Descricao, x.Meta, x.Progresso, x.RecompensaXp, x.Concluido,
                x.Concluido ? "Concluida" : protegerHoje ? "SemPressaoHoje" : "EmAndamento",
                x.Concluido
                    ? "Missao concluida com o progresso realmente registrado."
                    : protegerHoje
                        ? "Mantenha o progresso acumulado sem compensar hoje nem aumentar carga para buscar a meta."
                        : "Avance apenas pelas acoes ja coerentes com o plano e com a prontidao do dia."))
            .ToArray();

        var concluidas = itens.Count(x => x.Concluido);
        var semPressao = itens.Count(x => x.EstadoContextual == "SemPressaoHoje");
        const string seguranca = "Missoes Contextuais 2.0 nao removem progresso, nao reduzem XP, nao quebram streak por recuperacao planejada, nao mandam compensar sessoes e nao autorizam aumento automatico de carga, volume ou intensidade.";

        return new(estado, "Missoes Contextuais 2.0", foco, mensagem, itens.Length, concluidas, semPressao, itens, seguranca);
    }
}
