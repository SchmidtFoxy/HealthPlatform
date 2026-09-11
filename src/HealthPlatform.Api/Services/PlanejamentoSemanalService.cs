using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class PlanejamentoSemanalService
{
    public static PortalPlanejamentoSemanalResponse Montar(
        PortalCicloEsportivoResponse? ciclo,
        PortalMetasCicloResponse metas,
        PortalAcoesPrioritariasCicloResponse prioridades,
        PortalCheckpointCicloResponse checkpoint,
        PortalEstrategiaDiaResponse estrategia)
    {
        if (ciclo is null)
            return new("SemCiclo", "Planejamento da semana", "Nenhum ciclo ativo para organizar a semana.",
                null, null, null, Array.Empty<PortalPlanejamentoSemanalItemResponse>(),
                "O planejamento semanal organiza dados já existentes; não cria prescrição nova.");

        var itens = new List<PortalPlanejamentoSemanalItemResponse>();
        var treinosMeta = metas.Itens.FirstOrDefault(x => x.Codigo == "treinos-semana");
        int? realizados = null;
        int? meta = ciclo.MetaTreinosSemanais;
        int? restantes = null;

        if (treinosMeta is not null && meta.HasValue)
        {
            var token = treinosMeta.ValorAtual.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (int.TryParse(token, out var n)) realizados = n;
            restantes = Math.Max(0, meta.Value - (realizados ?? 0));
        }

        foreach (var p in prioridades.Prioridades.Take(3))
        {
            itens.Add(new(
                $"prioridade-{p.Codigo}",
                p.Categoria,
                p.Nivel == "Alta" ? "Prioridade" : "Foco",
                p.Titulo,
                p.Motivo,
                p.Acao));
        }

        if (restantes.HasValue)
        {
            var estadoTreino = restantes.Value == 0 ? "Concluido" : prioridades.Estado == "Revisar" ? "Contextualizar" : "Planejado";
            var tituloTreino = restantes.Value == 0
                ? "Meta semanal de treinos concluída"
                : $"{restantes.Value} sessão(ões) restante(s) na meta semanal";
            var orientacao = prioridades.Estado == "Revisar"
                ? "Use a meta apenas como referência. Recuperação e equilíbrio de carga têm precedência sobre completar volume semanal."
                : "Execute somente sessões já prescritas, respeitando a Estratégia do Dia e os limites de RPE definidos.";
            itens.Add(new("meta-treinos", "Treino", estadoTreino, tituloTreino,
                realizados.HasValue && meta.HasValue ? $"{realizados}/{meta} sessão(ões) concluída(s) nesta semana." : "Meta semanal do ciclo ativa.",
                orientacao));
        }

        if (checkpoint.Estado is "Revisar" or "Consolidar")
        {
            itens.Add(new("checkpoint", "Ciclo", checkpoint.Estado, checkpoint.Titulo,
                checkpoint.Resumo,
                checkpoint.Estado == "Revisar"
                    ? "Faça a revisão profissional antes de alterar treino ou nutrição."
                    : "Consolide o fechamento do ciclo antes de iniciar outra estratégia."));
        }

        itens.Add(new("estrategia-dia", "Hoje", "Contexto", "Use a Estratégia do Dia como limite",
            estrategia.Justificativa,
            $"Faixa de referência: RPE {estrategia.RpeMin}–{estrategia.RpeMax}. A semana não autoriza progressão automática."));

        var unicos = itens
            .GroupBy(x => x.Codigo)
            .Select(x => x.First())
            .Take(5)
            .ToList();

        var estado = prioridades.Estado == "Revisar" || checkpoint.Estado == "Revisar"
            ? "Proteger"
            : restantes == 0
                ? "Consolidar"
                : "Executar";

        var resumo = estado switch
        {
            "Proteger" => "A semana deve priorizar recuperação/revisão antes de perseguir metas de volume.",
            "Consolidar" => "A meta semanal principal já foi atendida; mantenha qualidade e recuperação.",
            _ => "Organize a semana em torno das sessões prescritas, metas do ciclo e sinais atuais de recuperação."
        };

        return new(estado, "Planejamento semanal adaptativo", resumo,
            meta, realizados, restantes, unicos,
            "Este planejamento não cria exercícios, não redistribui carga e não altera treino, nutrição ou medicação automaticamente.");
    }
}
