using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class CheckpointCicloService
{
    public static PortalCheckpointCicloResponse Montar(
        PortalCicloEsportivoResponse? ciclo,
        PortalMetasCicloResponse metas,
        PortalEvolucaoEsportivaResponse evolucao)
    {
        if (ciclo is null)
            return new("SemCiclo", "Checkpoint do ciclo", "Nenhum ciclo esportivo ativo para revisar.", 0, 0, 0m,
                Array.Empty<PortalCheckpointCicloItemResponse>(),
                "O checkpoint resume dados registrados; não cria ou altera prescrição profissional.");

        var itens = new List<PortalCheckpointCicloItemResponse>();
        var progressoTempo = ciclo.ProgressoTemporalPercentual;

        itens.Add(new("tempo-ciclo", "Ciclo", progressoTempo >= 85m ? "Fechamento" : "EmCurso",
            $"Semana {ciclo.SemanaAtual} de {ciclo.TotalSemanas}",
            $"{progressoTempo:0}% do período planejado já transcorreu.",
            progressoTempo >= 85m
                ? "Prepare a revisão de encerramento antes de iniciar um novo ciclo."
                : "Use o tempo como contexto; ele não determina sozinho se o plano deve mudar."));

        var metasMensuraveis = metas.Itens.Where(x => x.ProgressoPercentual.HasValue).ToList();
        if (metasMensuraveis.Count == 0)
        {
            itens.Add(new("metas-ciclo", "Metas", "DadosInsuficientes", "Metas mensuráveis",
                "O ciclo não possui metas mensuráveis suficientes para comparar com o avanço temporal.",
                "O profissional pode definir metas mensuráveis quando isso fizer sentido para o objetivo do ciclo."));
        }
        else
        {
            var mediaMetas = Math.Round(metasMensuraveis.Average(x => x.ProgressoPercentual!.Value), 1);
            var diferenca = mediaMetas - progressoTempo;
            var estadoMetas = metasMensuraveis.Any(x => x.Estado == "Observar") ? "Revisar"
                : diferenca >= -15m ? "Coerente" : "Acompanhar";
            itens.Add(new("metas-ciclo", "Metas", estadoMetas, "Metas x avanço do ciclo",
                $"Progresso médio das metas: {mediaMetas:0}% • tempo do ciclo: {progressoTempo:0}%.",
                estadoMetas == "Revisar"
                    ? "Revise o contexto das metas sinalizadas antes de qualquer ajuste."
                    : estadoMetas == "Acompanhar"
                        ? "As metas estão atrás do avanço temporal; observe a tendência antes de mudar a estratégia."
                        : "As metas mensuráveis acompanham o avanço temporal do ciclo."));
        }

        var atencao = evolucao.Indicadores.Where(x => x.Estado == "Atencao" || x.Estado == "Observar").ToList();
        itens.Add(new("evolucao-esportiva", "Evolução", atencao.Count > 0 ? "Revisar" : "Estavel",
            "Leitura multidimensional",
            atencao.Count > 0
                ? $"{atencao.Count} dimensão(ões) pedem atenção: {string.Join(", ", atencao.Take(3).Select(x => x.Titulo))}."
                : $"{evolucao.IndicadoresFavoraveis} dimensão(ões) favoráveis e nenhuma atenção relevante no painel atual.",
            atencao.Count > 0
                ? "Use as evidências de cada dimensão na revisão profissional; não trate o checkpoint como diagnóstico."
                : "Mantenha a estratégia prescrita e acompanhe a tendência ao longo das próximas semanas."));

        var revisar = itens.Any(x => x.Estado == "Revisar");
        var fechamento = progressoTempo >= 85m;
        var evoluindo = !revisar && evolucao.Estado == "Evoluindo";
        var estado = revisar ? "Revisar" : fechamento ? "Consolidar" : evoluindo ? "Evoluindo" : "EmCurso";
        var resumo = estado switch
        {
            "Revisar" => "Há evidências que merecem revisão profissional antes de ajustar a estratégia do ciclo.",
            "Consolidar" => "O ciclo está perto do encerramento; consolide resultados e prepare a revisão final.",
            "Evoluindo" => "O ciclo segue com sinais favoráveis e metas acompanhando o planejamento registrado.",
            _ => "O ciclo está em andamento; acompanhe metas, evolução e contexto sem reagir a um único dia."
        };

        return new(estado, "Checkpoint do ciclo", resumo, ciclo.SemanaAtual, ciclo.TotalSemanas, progressoTempo, itens,
            "Checkpoint não diagnostica, não prescreve e não altera treino, alimentação ou medicação automaticamente.");
    }
}
