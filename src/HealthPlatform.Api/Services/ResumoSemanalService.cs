using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class ResumoSemanalService
{
    public static PortalResumoSemanalResponse Montar(
        DateOnly dia,
        PortalPlanejamentoSemanalResponse planejamento,
        PortalAcoesPrioritariasCicloResponse prioridades,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalAdesaoNutricionalResponse nutricao,
        PortalHidratacaoContextualResponse hidratacao,
        PortalExecucaoDiaResponse execucao)
    {
        var deslocamento = ((int)dia.DayOfWeek + 6) % 7;
        var semanaInicio = dia.AddDays(-deslocamento);
        var semanaFim = semanaInicio.AddDays(6);
        var itens = new List<PortalResumoSemanalItemResponse>();

        var treinoEstado = planejamento.TreinosRestantesSemana == 0 ? "Concluido"
            : planejamento.Estado == "Proteger" ? "Contextualizar" : "EmAndamento";
        var treinoEvidencia = planejamento.MetaTreinosSemana.HasValue
            ? $"{planejamento.TreinosConcluidosSemana ?? 0}/{planejamento.MetaTreinosSemana} treino(s) da meta semanal."
            : "Sem meta semanal de treinos definida no ciclo.";
        itens.Add(new("treino", "Treino", treinoEstado, "Execução da meta semanal", treinoEvidencia));

        itens.Add(new("recuperacao", "Recuperação", recuperacao.NivelAtencao, "Recuperação recente",
            recuperacao.ProntidaoMedia7.HasValue
                ? $"Prontidão média 7 dias: {recuperacao.ProntidaoMedia7:0.#}/100 • {recuperacao.Mensagem}"
                : recuperacao.Mensagem));

        itens.Add(new("carga", "Carga", carga.NivelAtencao, "Carga de treino",
            carga.RelacaoCargaComBase.HasValue
                ? $"Relação com a linha de base: {carga.RelacaoCargaComBase:0.00}x • {carga.Mensagem}"
                : carga.Mensagem));

        itens.Add(new("nutricao", "Nutrição", nutricao.Estado, "Adesão nutricional",
            nutricao.AdequacaoRegistradaPercentual.HasValue
                ? $"{nutricao.AdequacaoRegistradaPercentual:0}% de adequação entre refeições registradas."
                : nutricao.Mensagem));

        itens.Add(new("hidratacao", "Hidratação", hidratacao.Estado, "Hidratação do dia",
            hidratacao.ProgressoPercentual.HasValue
                ? $"{hidratacao.ProgressoPercentual:0}% da meta hídrica registrada hoje."
                : hidratacao.Mensagem));

        itens.Add(new("execucao", "Execução", execucao.DiaFechado ? "Fechado" : "EmAndamento", "Roteiro de hoje",
            $"{execucao.ProgressoPercentual:0}% do roteiro diário registrado."));

        var precisaRevisar = prioridades.Estado == "Revisar"
            || string.Equals(recuperacao.NivelAtencao, "Alta", StringComparison.OrdinalIgnoreCase)
            || string.Equals(carga.NivelAtencao, "Alta", StringComparison.OrdinalIgnoreCase)
            || string.Equals(carga.Classificacao, "Revisar", StringComparison.OrdinalIgnoreCase);
        var observar = !precisaRevisar && (
            string.Equals(recuperacao.NivelAtencao, "Media", StringComparison.OrdinalIgnoreCase)
            || string.Equals(carga.NivelAtencao, "Media", StringComparison.OrdinalIgnoreCase)
            || string.Equals(planejamento.Estado, "Proteger", StringComparison.OrdinalIgnoreCase));

        var estado = precisaRevisar ? "Revisar"
            : observar ? "Observar"
            : planejamento.TreinosRestantesSemana == 0 ? "Equilibrada"
            : "Executando";

        var resumo = estado switch
        {
            "Revisar" => "A semana reúne sinais que merecem revisão profissional antes de perseguir mais volume ou intensidade.",
            "Observar" => "A semana segue em andamento, mas recuperação/carga merecem atenção antes de acelerar metas.",
            "Equilibrada" => "A principal meta semanal foi atendida sem sinal relevante de revisão nesta síntese.",
            _ => "A semana está em execução; use as metas como referência e preserve os limites definidos pela recuperação e pela Estratégia do Dia."
        };

        return new(estado, "Resumo semanal", resumo, semanaInicio, semanaFim,
            planejamento.TreinosConcluidosSemana, planejamento.MetaTreinosSemana, execucao.ProgressoPercentual,
            itens,
            "O resumo semanal descreve dados registrados; não cria score único, não diagnostica e não altera treino, nutrição ou medicação automaticamente.");
    }
}
