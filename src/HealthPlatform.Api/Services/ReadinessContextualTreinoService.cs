using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class ReadinessContextualTreinoService
{
    public static PortalReadinessContextualTreinoResponse Montar(
        PortalProntidaoDiariaResponse? prontidao,
        PortalEstrategiaDiaResponse estrategia,
        PortalDisponibilidadeTreinoResponse disponibilidade,
        PortalDeloadRecuperacaoPlanejadaResponse recuperacaoPlanejada)
    {
        const string seguranca = "Readiness contextual ao treino do dia e uma leitura descritiva: nao e liberacao medica, nao diagnostica lesao, nao altera a sessao automaticamente e nao autoriza aumento de carga ou volume. A decisao final permanece com atleta/profissional e com a prescricao existente.";

        var sessao = estrategia.SessoesPrevistas.FirstOrDefault();
        var sessaoPlanejada = string.IsNullOrWhiteSpace(sessao) ? "Sem sessao especifica registrada para hoje" : sessao;
        var demanda = ClassificarDemanda(estrategia);
        var criterios = new List<PortalCriterioReadinessContextualResponse>
        {
            new("sessao", string.IsNullOrWhiteSpace(sessao) ? "SemSessao" : "Planejada", "Sessao prevista", sessaoPlanejada),
            new("demanda", demanda, "Demanda contextual da sessao", $"RPE de referencia {estrategia.RpeMin}-{estrategia.RpeMax}; intensidade {estrategia.IntensidadeSugerida}."),
            new("disponibilidade", disponibilidade.Estado, "Disponibilidade do dia", disponibilidade.Resumo)
        };

        if (recuperacaoPlanejada.IntencaoPlanejada && recuperacaoPlanejada.Estado == "EmCurso")
            criterios.Add(new("deload", "Ativo", "Recuperacao planejada", "O bloco atual registra reducao deliberada de carga; a demanda da sessao deve ser interpretada dentro dessa intencao."));
        else
            criterios.Add(new("deload", "NaoAtivo", "Recuperacao planejada", "Nao ha janela ativa de deload/recuperacao planejada governando a sessao de hoje."));

        if (string.IsNullOrWhiteSpace(sessao))
            return new("SemSessaoPlanejada", "Readiness contextual ao treino do dia",
                "Nao existe uma sessao especifica registrada para hoje; a prontidao nao deve ser convertida em uma demanda que nao foi planejada.",
                sessaoPlanejada, demanda, prontidao?.Score, prontidao?.RecomendacaoTreino, false, false, criterios,
                "Mantenha a estrategia ja registrada e evite criar uma sessao apenas porque a prontidao parece favoravel.", seguranca);

        if (prontidao is null)
        {
            criterios.Add(new("prontidao", "SemDados", "Prontidao do dia", "Check-in de prontidao ainda nao registrado."));
            return new("DadosInsuficientes", "Readiness contextual ao treino do dia",
                "Ha uma sessao prevista, mas falta prontidao do dia para relacionar estado atual e demanda planejada.",
                sessaoPlanejada, demanda, null, null, false, false, criterios,
                "Registre o check-in antes de interpretar a compatibilidade com a sessao planejada; nao aumente exigencia para preencher dados.", seguranca);
        }

        criterios.Add(new("prontidao", prontidao.RecomendacaoTreino, "Prontidao do dia",
            $"{prontidao.Score}/100; recomendacao contextual base: {prontidao.RecomendacaoTreino}."));

        // A demanda planejada muda a interpretacao da mesma prontidao: readiness nao e um numero absoluto.
        // Disposicao alta nao apaga recuperacao, dor, carga ou uma janela de deload explicitamente planejada.
        if (recuperacaoPlanejada.IntencaoPlanejada && recuperacaoPlanejada.Estado == "EmCurso")
            return new("RecuperacaoPlanejada", "Readiness contextual ao treino do dia",
                "A prontidao deve ser interpretada dentro de uma janela deliberada de recuperacao, sem usar um bom dia para desfazer o deload.",
                sessaoPlanejada, demanda, prontidao.Score, prontidao.RecomendacaoTreino, false, true, criterios,
                "Preserve a intencao do bloco e ajuste somente dentro da prescricao/supervisao existente; nao compense carga reduzida.", seguranca);

        if (disponibilidade.Estado == "RecuperacaoPrioritaria")
            return new("RevisarAntesDaSessao", "Readiness contextual ao treino do dia",
                "Os sinais atuais pedem revisao antes de executar a demanda prevista, mesmo que exista disposicao subjetiva para treinar.",
                sessaoPlanejada, demanda, prontidao.Score, prontidao.RecomendacaoTreino, false, true, criterios,
                "Revise a sessao com foco em recuperacao/sintomas e mantenha a decisao sob julgamento profissional.", seguranca);

        var incompatibilidadePorDemanda = demanda switch
        {
            "Exigente" => prontidao.RecomendacaoTreino is "Leve" or "Recuperacao" || prontidao.Score < 75,
            "Moderada" => prontidao.RecomendacaoTreino == "Recuperacao" || prontidao.Score < 65,
            "Leve" => prontidao.RecomendacaoTreino == "Recuperacao" || prontidao.Score < 50,
            _ => false
        };

        if (disponibilidade.Estado == "Adaptar" || incompatibilidadePorDemanda)
            return new("AdaptarAoContexto", "Readiness contextual ao treino do dia",
                "A prontidao registrada nao sustenta executar esta demanda exatamente como planejada sem considerar adaptacao contextual.",
                sessaoPlanejada, demanda, prontidao.Score, prontidao.RecomendacaoTreino, false, true, criterios,
                "Use a estrategia do dia e a supervisao profissional para adaptar a sessao; adaptar nao significa falha de adesao.", seguranca);

        return new("CompativelComSessao", "Readiness contextual ao treino do dia",
            "A prontidao e os demais sinais registrados estao compativeis com a demanda ja planejada para hoje.",
            sessaoPlanejada, demanda, prontidao.Score, prontidao.RecomendacaoTreino, true, false, criterios,
            "Execute a sessao ja prescrita e observe a resposta. Compatibilidade nao e autorizacao para adicionar carga, volume ou intensidade.", seguranca);
    }

    private static string ClassificarDemanda(PortalEstrategiaDiaResponse estrategia)
    {
        if (estrategia.IntensidadeSugerida == "Recuperacao" || estrategia.RpeMax <= 4) return "Recuperacao";
        if (estrategia.IntensidadeSugerida == "Leve" || estrategia.RpeMax <= 6) return "Leve";
        if (estrategia.RpeMax >= 8 || estrategia.IntensidadeSugerida == "Pesado") return "Exigente";
        return "Moderada";
    }
}
