using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Traduz sinais do radar de adesão em poucos passos de retomada sustentável.
/// Não cria prescrição paralela, não exige compensação e não aplica punição de gamificação.
/// </summary>
public static class PlanoReconexaoService
{
    public static PortalPlanoReconexaoResponse Montar(
        PortalRadarAdesaoResponse radar,
        PortalPlanejamentoSemanalResponse planejamento,
        PortalAcoesPrioritariasCicloResponse prioridades,
        PortalExecucaoDiaResponse execucao)
    {
        var itens = new List<PortalPlanoReconexaoItemResponse>();

        if (radar.Estado == "DadosInsuficientes")
        {
            itens.Add(new("registrar", "Acompanhamento", 1, "Reconstrua o contexto",
                "Ainda faltam registros suficientes para reconhecer um padrão de continuidade.",
                "Registre o dia normalmente e deixe o sistema observar a rotina antes de sugerir retomadas."));
        }
        else if (radar.Estado == "Estavel")
        {
            var referencia = planejamento.Itens.FirstOrDefault();
            itens.Add(new("manter", "Continuidade", 1, "Mantenha o que já está funcionando",
                referencia?.Evidencia ?? "Não há sinal relevante de afastamento do plano nos dados disponíveis.",
                referencia?.Orientacao ?? "Siga o planejamento e a estratégia do dia sem adicionar compensações."));
        }
        else
        {
            foreach (var sinal in radar.Itens
                         .Where(x => x.Estado is "Atencao" or "Observar")
                         .Take(3))
            {
                itens.Add(new(sinal.Codigo, sinal.Categoria, itens.Count + 1, sinal.Titulo, sinal.Evidencia, sinal.Acao));
            }

            if (itens.Count < 3 && execucao.Pendentes > 0)
            {
                var proximo = execucao.Itens.FirstOrDefault(x => x.Status != "Concluido" && x.Obrigatorio)
                    ?? execucao.Itens.FirstOrDefault(x => x.Status != "Concluido");
                if (proximo is not null && itens.All(x => x.Codigo != "proxima-acao"))
                    itens.Add(new("proxima-acao", "Hoje", itens.Count + 1, "Faça só a próxima ação possível",
                        $"Roteiro de hoje: {execucao.Concluidos}/{execucao.TotalItens} item(ns) concluído(s).",
                        proximo.Acao));
            }

            if (itens.Count < 3)
            {
                var prioridade = prioridades.Prioridades
                    .FirstOrDefault(x => !string.Equals(x.Nivel, "Informativo", StringComparison.OrdinalIgnoreCase));
                if (prioridade is not null)
                    itens.Add(new("prioridade-ciclo", prioridade.Categoria, itens.Count + 1, prioridade.Titulo, prioridade.Motivo, prioridade.Acao));
            }
        }

        if (radar.ContextoRecuperacao && itens.Count == 0)
            itens.Add(new("recuperacao", "Recuperação", 1, "Proteja a recuperação",
                "O ritmo menor está coerente com sinais de recuperação/carga.",
                "Mantenha a redução prevista; não compense descanso com volume extra."));

        itens = itens.Take(3).Select((x, i) => x with { Ordem = i + 1 }).ToList();

        var estado = radar.Estado switch
        {
            "Reconectar" => "Retomar",
            "Observar" => "Ajustar",
            "DadosInsuficientes" => "Observar",
            _ => "Manter"
        };
        var titulo = estado switch
        {
            "Retomar" => "Retomada em passos pequenos",
            "Ajustar" => "Ajuste leve de continuidade",
            "Observar" => "Construa contexto antes de ajustar",
            _ => "Mantenha a continuidade"
        };
        var resumo = radar.ContextoRecuperacao
            ? "A recuperação continua acima da pressão por meta. O plano de reconexão não transforma descanso adequado em falha."
            : estado == "Retomar"
                ? "Escolha poucos comportamentos possíveis e retome sem tentar recuperar tudo de uma vez."
                : estado == "Ajustar"
                    ? "Uma pequena correção de rota é suficiente; não há motivo para compensações agressivas."
                    : "A rotina está suficientemente estável; preserve a estratégia atual.";

        return new(estado, titulo, resumo, itens.Count, itens,
            "Reconexão não é compensação: este plano não remove XP, não pune streak, não dobra treino, não restringe alimentação e não altera treino, nutrição ou medicação automaticamente.");
    }
}
