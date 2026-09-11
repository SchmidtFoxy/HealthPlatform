using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Monitora o contexto ao redor de uma progressao supervisionada sem atribuir causalidade automatica.
/// </summary>
public static class MonitoramentoRespostaProgressaoService
{
    public static PortalMonitoramentoRespostaProgressaoResponse Montar(
        PortalPlanoProgressaoSupervisionadaResponse plano,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalPerformanceResponse performance)
    {
        if (!plano.PodeAbrirDiscussao || plano.Estado != "Supervisionar" || string.IsNullOrWhiteSpace(plano.EixoSupervisionado))
        {
            return new("Aguardar", "Monitoramento ainda nao iniciado",
                "O acompanhamento de resposta so ganha contexto quando existe um eixo supervisionado em discussao.",
                false, null, "ManterContexto", Array.Empty<PortalSinalRespostaProgressaoResponse>(),
                "Mantenha a estrategia atual e reavalie quando o plano supervisionado estiver aberto.",
                "Sem plano supervisionado ativo, o sistema nao presume progressao nem resposta a intervencao.");
        }

        var sinais = new List<PortalSinalRespostaProgressaoResponse>();

        var estadoRecuperacao = recuperacao.Tendencia switch
        {
            "Atencao" => "Revisar",
            "Observar" => "Observar",
            "DadosInsuficientes" => "DadosInsuficientes",
            _ => "Favoravel"
        };
        sinais.Add(new("recuperacao", "Recuperacao", estadoRecuperacao, "Resposta de recuperacao", recuperacao.Mensagem));

        var estadoCarga = carga.Classificacao switch
        {
            "Revisar" => "Revisar",
            "AcimaDaBase" => "Observar",
            "SemBase" => "DadosInsuficientes",
            _ => "Favoravel"
        };
        sinais.Add(new("carga", "Carga", estadoCarga, "Carga recente e linha de base", carga.Mensagem));

        var estadoPerformance = performance.Tendencia switch
        {
            "DadosInsuficientes" => "DadosInsuficientes",
            "VolumeMaior" => "Observar",
            _ => "Contexto"
        };
        sinais.Add(new("performance", "Performance", estadoPerformance, "Performance no periodo", performance.Mensagem));

        string estado;
        string decisao;
        string resumo;
        string acao;

        if (sinais.Any(x => x.Estado == "Revisar"))
        {
            estado = "Revisar"; decisao = "NaoProgredirAgora";
            resumo = "Ha sinal de recuperacao ou carga que merece revisao antes de qualquer nova mudanca.";
            acao = "Revise o contexto com o profissional e preserve os demais eixos; nao some outra progressao agora.";
        }
        else if (sinais.Any(x => x.Estado == "DadosInsuficientes"))
        {
            estado = "DadosInsuficientes"; decisao = "ObservarMais";
            resumo = "Ainda faltam dados para interpretar a resposta com seguranca.";
            acao = "Colete novos registros sem aumentar exigencia apenas para produzir dados.";
        }
        else if (sinais.Any(x => x.Estado == "Observar"))
        {
            estado = "Observar"; decisao = "ManterUmaMudanca";
            resumo = "O contexto pode ser acompanhado, mas ainda ha oscilacao que recomenda manter uma unica mudanca.";
            acao = "Mantenha um eixo por vez e observe a tendencia antes de discutir novo avanco.";
        }
        else
        {
            estado = "Estavel"; decisao = "ContinuarObservacao";
            resumo = "Recuperacao e carga nao mostram sinal atual que obrigue revisao; continue observando antes de nova mudanca.";
            acao = "Preserve a estrategia atual e leve a resposta acumulada para a proxima reavaliacao profissional.";
        }

        return new(estado, "Monitoramento de resposta a progressao", resumo, true, plano.EixoSupervisionado,
            decisao, sinais, acao,
            "O sistema monitora contexto, nao prova causalidade: sem registro temporal da mudanca, nao atribui melhora ou piora a progressao e nao autoriza aumento automatico de carga, volume, calorias ou medicacao.");
    }
}
