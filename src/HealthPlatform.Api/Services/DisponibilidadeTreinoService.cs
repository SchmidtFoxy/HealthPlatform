using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class DisponibilidadeTreinoService
{
    public static PortalDisponibilidadeTreinoResponse Montar(
        PortalProntidaoDiariaResponse? prontidao,
        PortalDorCorporalResumoResponse dor,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalEstrategiaDiaResponse estrategia)
    {
        // Disponibilidade para treino separa vontade/disposicao de compatibilidade com a sessao planejada.
        // Disposicao alta nao apaga dor, recuperacao desfavoravel ou carga que pede revisao.
        var criterios = new List<PortalCriterioDisponibilidadeTreinoResponse>();
        var sessao = $"{estrategia.IntensidadeSugerida} • RPE {estrategia.RpeMin}-{estrategia.RpeMax}";

        if (prontidao is null)
        {
            criterios.Add(new("prontidao", "SemDados", "Prontidao do dia", "Sem check-in",
                "Sem o check-in diario nao e possivel comparar o estado atual com a sessao planejada."));
            criterios.Add(new("recuperacao", recuperacao.Tendencia, "Recuperacao recente", recuperacao.Tendencia, recuperacao.Mensagem));
            criterios.Add(new("carga", carga.Classificacao, "Carga recente", carga.Classificacao, carga.Mensagem));
            return new PortalDisponibilidadeTreinoResponse(
                "DadosInsuficientes", "Disponibilidade para treino", "Falta o check-in do dia para interpretar a sessao planejada com seguranca contextual.",
                sessao, false, criterios,
                "Registre o check-in antes de interpretar se a sessao planejada combina com o estado de hoje.",
                "Disponibilidade para treino nao e liberacao medica, nao diagnostica lesao e nao prescreve alteracao automatica da sessao.");
        }

        criterios.Add(new("disposicao", prontidao.DisposicaoNivel >= 7 ? "Alta" : prontidao.DisposicaoNivel >= 5 ? "Moderada" : "Baixa",
            "Disposicao percebida", $"{prontidao.DisposicaoNivel}/10",
            "Disposicao representa vontade/percepcao subjetiva e nao substitui recuperacao, dor ou carga."));
        criterios.Add(new("prontidao", prontidao.RecomendacaoTreino, "Prontidao do dia", $"{prontidao.Score}/100", prontidao.MotivoRecomendacao ?? "Leitura do check-in diario."));
        criterios.Add(new("dor", dor.NivelAtencao, "Dor localizada", $"{dor.IntensidadeMaxima7}/10 max.", dor.Mensagem));
        criterios.Add(new("recuperacao", recuperacao.Tendencia, "Recuperacao recente", recuperacao.Tendencia, recuperacao.Mensagem));
        criterios.Add(new("carga", carga.Classificacao, "Carga recente", carga.Classificacao, carga.Mensagem));

        var recuperacaoPrioritaria =
            dor.IntensidadeMaxima7 >= 7 || dor.ImpactoMaximoTreino7 >= 7 ||
            recuperacao.Tendencia == "Atencao" || carga.Classificacao == "Revisar" ||
            prontidao.RecomendacaoTreino == "Recuperacao";

        var adaptar = !recuperacaoPrioritaria && (
            dor.IntensidadeMaxima7 >= 4 || dor.ImpactoMaximoTreino7 >= 4 ||
            recuperacao.Tendencia == "Observar" || carga.Classificacao == "AcimaDaBase" ||
            prontidao.Score < 65 || prontidao.RecomendacaoTreino == "Leve");

        if (recuperacaoPrioritaria)
            return new PortalDisponibilidadeTreinoResponse(
                "RecuperacaoPrioritaria", "Disponibilidade para treino",
                "Ha sinais atuais que devem prevalecer sobre a vontade de cumprir a sessao exatamente como planejada.",
                sessao, true, criterios,
                "Revise a sessao com foco em recuperacao e sintomas antes de manter intensidade/volume planejados.",
                "Esta leitura nao e diagnostico, nao e liberacao medica e nao substitui avaliacao profissional. O sistema nao altera treino automaticamente.");

        if (adaptar)
            return new PortalDisponibilidadeTreinoResponse(
                "Adaptar", "Disponibilidade para treino",
                "A sessao pode exigir adaptacao ao contexto atual; motivacao isolada nao deve decidir intensidade.",
                sessao, true, criterios,
                "Use a estrategia do dia e o julgamento profissional para adaptar sem compensar ou aumentar exigencia.",
                "Adaptar nao significa incapacidade ou lesao. A leitura nao diagnostica, nao prescreve e nao libera treino automaticamente.");

        return new PortalDisponibilidadeTreinoResponse(
            "CompativelComPlanejado", "Disponibilidade para treino",
            "Os sinais registrados estao compativeis com manter a estrategia planejada, sem indicar necessidade automatica de progressao.",
            sessao, false, criterios,
            "Execute o que ja esta planejado e continue observando a resposta; nao aumente carga apenas porque o dia parece favoravel.",
            "Compativel com o planejado nao significa liberacao medica nem autorizacao para progredir. O sistema nao prescreve mudanca automatica.");
    }
}
