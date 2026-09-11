using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Integra a resposta observada ao longo da janela registrada sem criar score unico ou inferir causalidade.
/// Cada eixo permanece visivel e independente para apoiar a interpretacao profissional.
/// </summary>
public static class InterpretacaoLongitudinalProgressaoService
{
    public static PortalInterpretacaoLongitudinalProgressaoResponse Montar(
        PortalRegistroProgressaoResponse registro,
        PortalComparativoProgressaoResponse comparativo,
        PortalReavaliacaoProgressaoResponse reavaliacao)
    {
        if (!comparativo.PossuiMarcoTemporal || registro.EventoAtual is null)
            return new("DadosInsuficientes", "Interpretacao longitudinal ainda indisponivel",
                "Ainda nao existe um marco temporal registrado suficiente para interpretar a resposta longitudinal.",
                null, null, false, Array.Empty<PortalInterpretacaoLongitudinalEixoResponse>(),
                "Mantenha o registro habitual sem aumentar exigencia para produzir dados.",
                "Sem evento realmente aplicado, o sistema nao fabrica um padrao longitudinal.",
                "Ausencia de dados nao e falha e nao autoriza progressao automatica.");

        var eixos = comparativo.Eixos
            .Where(x => x.Codigo is "prontidao" or "recuperacao" or "dor" or "carga" or "volume")
            .Select(x => new PortalInterpretacaoLongitudinalEixoResponse(
                x.Codigo, x.Titulo, x.Estado,
                $"Antes: {(x.Antes.HasValue ? x.Antes.Value.ToString("0.0") : "sem dado")} | depois: {(x.Depois.HasValue ? x.Depois.Value.ToString("0.0") : "sem dado")}. {x.Evidencia}"))
            .ToList();

        if (comparativo.Estado == "DadosInsuficientes" || comparativo.Estado == "SemMarco")
            return new("DadosInsuficientes", "Resposta longitudinal com dados insuficientes",
                "O evento existe, mas ainda faltam tempo ou registros para interpretar um padrao com seguranca.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, false, eixos,
                "Continue observando a rotina normal sem aumentar carga, volume ou meta apenas para completar a janela.",
                "Dados incompletos permanecem explicitamente incompletos; nao sao convertidos em resposta boa ou ruim.",
                "O sistema nao calcula score de sucesso e nao conclui causalidade.");

        if (comparativo.Estado == "JanelaEmFormacao")
            return new("EmFormacao", "Padrao longitudinal em formacao",
                "Ja existem sinais apos o evento, mas a janela ainda precisa amadurecer antes de uma leitura mais firme.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, false, eixos,
                "Mantenha uma unica mudanca em observacao e preserve os demais eixos estaveis.",
                "Sinais iniciais ajudam a acompanhar, mas nao devem ser tratados como tendencia consolidada.",
                "Mudanca temporal precoce nao prova beneficio, dano ou resposta causada pela progressao.");

        var clinicos = comparativo.Eixos.Where(x => x.Codigo is "prontidao" or "recuperacao" or "dor").ToList();
        var favoraveis = clinicos.Count(x => x.Estado == "MudancaFavoravel");
        var desfavoraveis = clinicos.Count(x => x.Estado == "MudancaDesfavoravel");
        var insuficientes = clinicos.Count(x => x.Estado == "DadosInsuficientes");

        if (reavaliacao.Estado == "Revisar" || desfavoraveis >= 2)
            return new("Atencao", "Padrao longitudinal pede revisao",
                "Ha sinais clinicos ou de recuperacao que merecem revisao antes de qualquer nova discussao de progressao.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, true, eixos,
                "Revise o contexto com o profissional e nao some outra progressao enquanto os sinais de atencao persistirem.",
                "Recuperacao e dor prevalecem sobre leitura isolada de carga ou performance.",
                "Atencao nao e diagnostico; o sistema nao ajusta tratamento, medicacao, carga ou dieta automaticamente.");

        if (insuficientes >= 2)
            return new("DadosInsuficientes", "Padrao longitudinal parcialmente observado",
                "A janela temporal esta formada, mas eixos clinicos relevantes ainda possuem registros insuficientes.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, true, eixos,
                "Mantenha a estrategia atual e complete registros quando eles ocorrerem naturalmente.",
                "Janela completa no calendario nao substitui qualidade e cobertura dos dados.",
                "Falta de dado nao e interpretada como estabilidade, sucesso ou falha.");

        if (favoraveis >= 2 && desfavoraveis == 0)
            return new("Favoravel", "Padrao longitudinal favoravel",
                "Os principais eixos clinicos observados se moveram em direcao favoravel nesta janela temporal.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, true, eixos,
                "Preserve a estrategia e leve o padrao acumulado para reavaliacao profissional antes de qualquer nova mudanca.",
                "Favoravel descreve o padrao observado; nao significa sucesso causal da progressao.",
                "Padrao favoravel nao libera aumento automatico de carga, volume, calorias, meta ou medicacao.");

        if (favoraveis > 0 && desfavoraveis > 0)
            return new("Misto", "Padrao longitudinal misto",
                "Os eixos nao caminham todos na mesma direcao; a resposta deve ser interpretada por componente.",
                comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, true, eixos,
                "Evite resumir o quadro em uma nota unica; revise cada eixo e o contexto do ciclo separadamente.",
                "Resposta mista e informacao clinica util, nao falha de adesao nem sinal automatico para compensar.",
                "O sistema nao cria score composto nem transforma divergencia entre eixos em prescricao.");

        return new("Estavel", "Padrao longitudinal estavel",
            "Nao ha mudanca clara suficiente nos eixos clinicos observados para classificar o periodo como favoravel ou de atencao.",
            comparativo.EixoProgressao, comparativo.DataAplicacaoUtc, true, eixos,
            "Mantenha a observacao e interprete estabilidade junto do objetivo esportivo e do contexto profissional.",
            "Estabilidade nao significa ausencia de efeito nem prova de que a progressao foi adequada.",
            "Estavel nao autoriza nova progressao automatica e nao aumenta meta por conta propria.");
    }
}
