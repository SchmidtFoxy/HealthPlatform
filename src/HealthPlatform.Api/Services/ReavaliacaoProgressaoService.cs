using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Converte o monitoramento de resposta em uma decisao de continuidade sem inferir sucesso causal.
/// </summary>
public static class ReavaliacaoProgressaoService
{
    public static PortalReavaliacaoProgressaoResponse Montar(
        PortalPlanoProgressaoSupervisionadaResponse plano,
        PortalMonitoramentoRespostaProgressaoResponse monitoramento)
    {
        if (!plano.PodeAbrirDiscussao || !monitoramento.EmAcompanhamento || string.IsNullOrWhiteSpace(monitoramento.EixoSupervisionado))
        {
            return new("Manter", "Reavaliacao sem progressao ativa",
                "Nao ha uma progressao supervisionada ativa para encerrar ou ampliar nesta leitura.",
                false, false, null, "ManterContexto", Array.Empty<PortalCriterioReavaliacaoProgressaoResponse>(),
                "Mantenha a estrategia atual e reavalie quando houver um eixo supervisionado em acompanhamento.",
                "Sem progressao ativa, nenhuma nova mudanca e criada pelo sistema.",
                "Reavaliacao nao cria prescricao, meta ou progressao automatica.");
        }

        var criterios = monitoramento.Sinais
            .Select(x => new PortalCriterioReavaliacaoProgressaoResponse(x.Codigo, x.Titulo, x.Estado, x.Evidencia))
            .ToList();

        if (monitoramento.Estado == "Revisar")
        {
            return new("Revisar", "Progressao precisa de revisao",
                "A resposta atual contem sinal que deve ser revisto antes de manter ou encerrar a janela de observacao.",
                true, false, monitoramento.EixoSupervisionado, "RevisarComProfissional", criterios,
                "Revise recuperacao, carga e contexto do eixo supervisionado; nao some outra progressao agora.",
                "A decisao seguinte deve ocorrer depois da revisao profissional e de novo contexto observavel.",
                "Sinal de revisao prevalece sobre performance favoravel; o sistema nao ajusta carga, volume, calorias ou medicacao.");
        }

        if (monitoramento.Estado == "DadosInsuficientes")
        {
            return new("AguardarDados", "Ainda faltam dados para reavaliar",
                "A janela atual nao deve ser encerrada nem ampliada com base em informacao incompleta.",
                true, false, monitoramento.EixoSupervisionado, "ObservarMais", criterios,
                "Continue registrando a rotina normal sem aumentar exigencia apenas para produzir dados.",
                "Reavalie quando houver contexto suficiente nos eixos monitorados.",
                "Falta de dado nao e falha e nao autoriza progressao, compensacao ou aumento automatico de meta.");
        }

        if (monitoramento.Estado == "Observar")
        {
            return new("Manter", "Mantenha a observacao atual",
                "Ainda existe oscilacao suficiente para manter uma unica mudanca em observacao.",
                true, false, monitoramento.EixoSupervisionado, "ManterUmaMudanca", criterios,
                "Preserve o eixo atual sem abrir outra frente de progressao.",
                "Reavalie depois de nova leitura de recuperacao, carga e performance.",
                "Manter nao significa aumentar: uma nova progressao nunca e criada automaticamente.");
        }

        return new("Encerrar", "Janela atual pode ser encerrada",
            "O contexto atual esta estavel o suficiente para encerrar esta janela de observacao, sem concluir causalidade ou sucesso da progressao.",
            true, true, monitoramento.EixoSupervisionado, "EncerrarObservacao", criterios,
            "Encerre apenas a observacao atual e retorne o eixo para manutencao; nao abra outra progressao na mesma decisao.",
            "Uma nova discussao de progressao exige uma nova janela e nova avaliacao independente.",
            "Encerrar observacao nao prova que a progressao causou melhora, nao aumenta meta e nao libera automaticamente novo desafio.");
    }
}
