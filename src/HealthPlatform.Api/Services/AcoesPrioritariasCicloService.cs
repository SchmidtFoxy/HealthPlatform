using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class AcoesPrioritariasCicloService
{
    public static PortalAcoesPrioritariasCicloResponse Montar(
        PortalCicloEsportivoResponse? ciclo,
        PortalTendenciaObjetivoResponse tendenciaObjetivo,
        PortalCheckpointCicloResponse checkpoint,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalAdesaoNutricionalResponse nutricao,
        PortalHidratacaoContextualResponse hidratacao)
    {
        if (ciclo is null)
            return new("SemCiclo", "Prioridades do ciclo", "Nenhum ciclo ativo para ordenar prioridades.",
                Array.Empty<PortalAcaoPrioritariaCicloItemResponse>(),
                "As prioridades organizam sinais já existentes e não criam prescrição nova.");

        var candidatos = new List<(int Peso, string Codigo, string Categoria, string Nivel, string Titulo, string Motivo, string Acao)>();

        if (recuperacao.NivelAtencao is "Alta" or "Atencao" || recuperacao.Tendencia is "Piorando" or "Queda")
            candidatos.Add((100, "recuperacao", "Recuperação", "Alta", "Proteja a recuperação",
                recuperacao.Mensagem,
                "Priorize a estratégia recuperativa já definida e evite acrescentar carga ou volume fora do plano."));

        if (carga.Classificacao == "Revisar" || carga.NivelAtencao is "Alta" or "Atencao")
            candidatos.Add((95, "carga", "Carga", "Alta", "Revise o equilíbrio de carga",
                carga.Mensagem,
                "Mantenha a carga dentro da prescrição e use a revisão profissional antes de qualquer progressão."));

        var objetivoAtencao = tendenciaObjetivo.Itens.FirstOrDefault(x => x.Estado == "Atencao");
        if (objetivoAtencao is not null)
            candidatos.Add((85, $"objetivo-{objetivoAtencao.Codigo}", objetivoAtencao.Eixo, "Media", objetivoAtencao.Titulo,
                objetivoAtencao.Evidencia,
                "Concentre a execução da semana neste eixo sem compensações nem mudanças automáticas de prescrição."));

        if (checkpoint.Estado == "Revisar")
            candidatos.Add((90, "checkpoint", "Ciclo", "Alta", "Faça uma revisão do ciclo",
                checkpoint.Resumo,
                "Use o checkpoint como pauta para revisão profissional antes de ajustar treino ou nutrição."));
        else if (checkpoint.Estado == "Consolidar")
            candidatos.Add((70, "checkpoint", "Ciclo", "Media", "Consolide o fechamento do ciclo",
                checkpoint.Resumo,
                "Feche o ciclo atual antes de iniciar uma nova estratégia."));

        if (nutricao.Estado is "Revisar" or "Parcial")
            candidatos.Add((60, "nutricao", "Nutrição", "Media", "Recupere a execução alimentar",
                nutricao.Mensagem,
                "Retome as refeições previstas sem compensar com jejum, restrição ou excesso."));

        if (hidratacao.NivelAtencao is "Alta" or "Media")
            candidatos.Add((55, "hidratacao", "Hidratação", "Media", "Complete a meta hídrica definida",
                hidratacao.Mensagem,
                "Distribua ao longo do dia o restante da meta já definida; não aumente a meta automaticamente pelo treino."));

        if (candidatos.Count == 0)
            candidatos.Add((40, "execucao", "Ciclo", "Baixa", "Execute o plano com consistência",
                tendenciaObjetivo.Resumo,
                "Siga o plano do ciclo e mantenha registros consistentes para sustentar decisões futuras."));

        var prioridades = candidatos
            .OrderByDescending(x => x.Peso)
            .ThenBy(x => x.Codigo)
            .Take(3)
            .Select((x, i) => new PortalAcaoPrioritariaCicloItemResponse(x.Codigo, x.Categoria, i + 1, x.Nivel, x.Titulo, x.Motivo, x.Acao))
            .ToList();

        var estado = prioridades.Any(x => x.Nivel == "Alta") ? "Revisar" : prioridades.Any(x => x.Nivel == "Media") ? "Priorizar" : "Executar";
        var resumo = estado switch
        {
            "Revisar" => "Há sinais que devem ficar acima de metas de volume ou performance nesta semana.",
            "Priorizar" => "Há poucos eixos claros para priorizar sem mudar a prescrição.",
            _ => "Sem sinais relevantes de revisão; o foco é executar o plano com consistência."
        };

        return new(estado, "Prioridades da semana", resumo, prioridades,
            "A ordem prioriza segurança e aderência; não diagnostica, não prescreve e não altera treino, nutrição ou medicação automaticamente.");
    }
}
