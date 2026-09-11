using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Descreve padroes individuais recorrentes a partir de eventos realmente registrados do proprio atleta.
/// Nao calcula risco, nao define dose de progressao e nao transforma historico em prescricao automatica.
/// </summary>
public static class ToleranciaProgressaoService
{
    // EventosInterpretaveis permanecem separados do total para nao mascarar lacunas historicas.
    private static readonly HashSet<string> EstadosAtencao = new(StringComparer.OrdinalIgnoreCase)
    { "Atencao", "Misto", "Revisar" };

    private static readonly HashSet<string> EstadosEstaveis = new(StringComparer.OrdinalIgnoreCase)
    { "Estavel", "Favoravel" };

    public static PortalToleranciaProgressaoResponse Montar(PortalHistoricoProgressaoResponse historico)
    {
        if (historico.Eventos.Count < 2)
            return new("DadosInsuficientes", "Tolerancia individual a progressao ainda em formacao",
                "Sao necessarios eventos repetidos para descrever um padrao individual sem extrapolar um episodio isolado.",
                0, Array.Empty<PortalToleranciaProgressaoEixoResponse>(),
                "Um unico evento nao define tolerancia individual.",
                "Tolerancia aqui significa padrao observado; nao e score de risco, diagnostico nem recomendacao de carga.");

        var eixos = historico.Eventos
            .GroupBy(x => x.Eixo, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => MontarEixo(g.Key, g.OrderBy(x => x.DataAplicacaoUtc).ToList()))
            .ToList();

        if (eixos.Count == 0)
            return new("DadosInsuficientes", "Tolerancia individual a progressao ainda em formacao",
                "Ha historico, mas ainda nao existem dois eventos do mesmo eixo para observar recorrencia individual.",
                0, eixos,
                "O sistema espera recorrencia dentro do mesmo eixo antes de descrever um padrao.",
                "Nao mistura eixos, nao calcula risco e nao recomenda intensidade de progressao.");

        var estado = eixos.Any(x => x.Padrao == "RequerContexto") ? "PadraoComAtencao" : "PadraoDescritivo";
        return new(estado, "Tolerancia individual a progressao",
            $"{eixos.Count} eixo(s) possuem recorrencia suficiente para uma leitura descritiva do proprio atleta.",
            eixos.Count, eixos,
            "A leitura resume recorrencias do historico individual e deve ser interpretada junto do ciclo, objetivo, recuperacao e contexto clinico-esportivo.",
            "Padrao historico nao e limite biologico fixo, nao e probabilidade de lesao e nao autoriza nova progressao automaticamente.");
    }

    private static PortalToleranciaProgressaoEixoResponse MontarEixo(string eixo, IReadOnlyCollection<PortalHistoricoProgressaoItemResponse> eventos)
    {
        var interpretaveis = eventos.Where(x => !string.Equals(x.EstadoResposta, "HistoricoRegistrado", StringComparison.OrdinalIgnoreCase)).ToList();
        var mediaDias = eventos.Count > 0 ? Math.Round((decimal)eventos.Average(x => x.DiasObservacao), 1) : (decimal?)null;
        var atencao = interpretaveis.Count(x => EstadosAtencao.Contains(x.EstadoResposta));
        var estaveis = interpretaveis.Count(x => EstadosEstaveis.Contains(x.EstadoResposta));

        string padrao;
        string leitura;
        if (interpretaveis.Count == 0)
        {
            padrao = "HistoricoSemResposta";
            leitura = "Existem eventos repetidos, mas ainda faltam leituras de resposta comparaveis para caracterizar tolerancia.";
        }
        else if (atencao >= 2 && atencao > estaveis)
        {
            padrao = "RequerContexto";
            leitura = "Respostas que pediram atencao aparecem de forma recorrente neste eixo; revise contexto, recuperacao e momento do ciclo antes de novas mudancas.";
        }
        else if (estaveis >= 2 && atencao == 0)
        {
            padrao = "EstabilidadeRecorrente";
            leitura = "Ha recorrencia de respostas estaveis/favoraveis documentadas neste eixo, sem transformar isso em autorizacao automatica para aumentar exigencia.";
        }
        else
        {
            padrao = "Variavel";
            leitura = "As respostas variam entre eventos; o contexto de cada progressao continua mais importante do que uma media isolada.";
        }

        var evidencias = new List<string>
        {
            $"{eventos.Count} evento(s) realmente registrado(s) neste eixo.",
            mediaDias.HasValue ? $"Media descritiva de {mediaDias.Value:0.#} dia(s) em observacao." : "Duracao de observacao indisponivel.",
            $"{interpretaveis.Count} evento(s) com resposta interpretavel; {estaveis} estavel/favoravel e {atencao} com atencao/misto/revisao."
        };

        return new(eixo, eventos.Count, interpretaveis.Count, mediaDias, padrao, leitura, evidencias);
    }
}
