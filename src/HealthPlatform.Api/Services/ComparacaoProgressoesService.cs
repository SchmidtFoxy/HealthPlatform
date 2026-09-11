using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Compara eventos realmente registrados apenas dentro do mesmo eixo esportivo.
/// Nao ranqueia progressoes, nao escolhe melhor/pior evento e nao transforma diferenca temporal em causalidade.
/// </summary>
public static class ComparacaoProgressoesService
{
    public static PortalComparacaoProgressaoResponse Montar(PortalHistoricoProgressaoResponse historico)
    {
        if (historico.Eventos.Count == 0)
            return new("SemHistorico", "Comparacao entre progressoes ainda indisponivel",
                "Ainda nao existem eventos registrados para formar uma comparacao longitudinal.",
                0, 0, Array.Empty<PortalComparacaoProgressaoEixoResponse>(),
                "A comparacao nasce do historico persistido e nunca fabrica uma progressao para completar pares.",
                "Sem historico nao ha ranking, score ou inferencia de resposta.");

        var grupos = historico.Eventos
            .GroupBy(x => x.Eixo, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var eventos = g.OrderByDescending(x => x.DataAplicacaoUtc).Take(4).ToList();
                var itens = eventos.Select(x => new PortalComparacaoProgressaoEventoResponse(
                    x.Id, x.DataAplicacaoUtc, x.Status, x.DiasObservacao, x.EstadoResposta, x.ResumoResposta)).ToList();

                if (eventos.Count < 2)
                    return new PortalComparacaoProgressaoEixoResponse(g.Key, g.Count(), false, itens,
                        "Existe apenas um evento neste eixo; ele permanece documentado sem comparacao artificial.");

                var atual = eventos[0];
                var anterior = eventos[1];
                var deltaDias = atual.DiasObservacao - anterior.DiasObservacao;
                var duracao = deltaDias == 0
                    ? "As duas janelas tiveram a mesma duracao observada."
                    : deltaDias > 0
                        ? $"A janela mais recente teve {deltaDias} dia(s) a mais de observacao."
                        : $"A janela mais recente teve {Math.Abs(deltaDias)} dia(s) a menos de observacao.";

                var resposta = string.Equals(atual.EstadoResposta, anterior.EstadoResposta, StringComparison.OrdinalIgnoreCase)
                    ? $"O estado de resposta permaneceu {atual.EstadoResposta}."
                    : $"O estado documentado mudou de {anterior.EstadoResposta} para {atual.EstadoResposta}.";

                return new PortalComparacaoProgressaoEixoResponse(g.Key, g.Count(), true, itens,
                    $"{duracao} {resposta} Compare contexto, objetivo e momento do ciclo antes de interpretar a diferenca.");
            })
            .ToList();

        var comparaveis = grupos.Count(x => x.PossuiComparacao);
        var estado = comparaveis > 0 ? "Comparavel" : "HistoricoSemPar";
        var resumo = comparaveis > 0
            ? $"{comparaveis} eixo(s) possuem pelo menos dois eventos realmente registrados para comparacao contextual."
            : "Ha historico registrado, mas ainda nao existem dois eventos do mesmo eixo para comparar.";

        return new(estado, "Comparacao entre progressoes", resumo, grupos.Count, comparaveis, grupos,
            "Compare apenas eventos do mesmo eixo. Duracao, status e resposta observada sao contexto; nenhum deles define sozinho qual progressao foi melhor.",
            "A comparacao nao ranqueia progressoes, nao cria score de sucesso, nao prova causalidade e nao autoriza nova progressao automaticamente.");
    }
}
