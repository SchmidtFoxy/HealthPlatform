using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Protege uma retomada recente contra nova sobrecarga de cobrança.
/// Usa sinais observáveis de continuidade; não rotula recaída, não cria probabilidade e não aplica punição.
/// </summary>
public static class ProtecaoRetomadaService
{
    public static PortalProtecaoRetomadaResponse Montar(
        PortalRadarAdesaoResponse radar,
        PortalPlanoReconexaoResponse reconexao,
        PortalTendenciaSemanalResponse tendenciaSemanal,
        PortalGamificacaoResponse gamificacao)
    {
        var itens = new List<PortalProtecaoRetomadaItemResponse>();
        var diasAtivos = tendenciaSemanal.Itens.FirstOrDefault(x => x.Codigo == "dias-ativos");

        // Retomada observável: sequência curta voltando ou mais dias ativos que no período comparável,
        // enquanto a consistência ainda não chegou à faixa mais estável.
        var retomadaEmCurso = radar.Estado != "DadosInsuficientes" &&
            gamificacao.ConsistenciaScore < 80 &&
            ((gamificacao.StreakDias >= 1 && gamificacao.StreakDias <= 4) || diasAtivos?.Estado == "Maior");

        if (radar.Estado == "DadosInsuficientes")
        {
            itens.Add(new("contexto", "Continuidade", "Contexto", "Construa contexto antes de intervir",
                "Ainda não há registros suficientes para reconhecer uma retomada ou nova oscilação.",
                "Continue registrando a rotina normal; não crie compensações preventivas sem evidência."));
        }
        else
        {
            if (retomadaEmCurso)
                itens.Add(new("retomada", "Continuidade", "Protecao", "Proteja a retomada recente",
                    $"Streak atual: {gamificacao.StreakDias} dia(s) • consistência {gamificacao.ConsistenciaScore}/100.",
                    "Mantenha poucos comportamentos repetíveis antes de adicionar novas cobranças ou volume."));

            if (!radar.ContextoRecuperacao && diasAtivos?.Estado == "Menor")
                itens.Add(new("ritmo", "Execução", "Observar", "Ritmo voltou a cair",
                    $"Dias ativos: {diasAtivos.ValorAtual} vs {diasAtivos.ValorAnterior} no mesmo intervalo semanal.",
                    "Retome apenas a próxima ação possível do plano; não tente recuperar dias perdidos de uma vez."));

            if (radar.ContextoRecuperacao)
                itens.Add(new("recuperacao", "Recuperação", "Contexto", "Não confunda proteção com recaída",
                    "Há contexto de recuperação/carga justificando redução de ritmo.",
                    "Preserve a redução prevista; descanso coerente continua sendo parte do plano."));

            foreach (var sinal in radar.Itens
                         .Where(x => (x.Estado is "Atencao" or "Observar") && (x.Codigo is "nutricao" or "hidratacao"))
                         .Take(2))
            {
                if (itens.All(x => x.Codigo != sinal.Codigo))
                    itens.Add(new(sinal.Codigo, sinal.Categoria, "Observar", sinal.Titulo, sinal.Evidencia, sinal.Acao));
            }

            if (reconexao.Estado == "Retomar" && itens.All(x => x.Codigo != "reconexao"))
                itens.Add(new("reconexao", "Continuidade", "Protecao", "Ainda é fase de reconexão",
                    "O plano atual já está em modo de retomada com poucos passos.",
                    "Conclua o passo possível antes de adicionar novas metas ou compensações."));
        }

        itens = itens.Take(3).ToList();
        var sinaisFragilidade = itens.Count(x => x.Estado is "Protecao" or "Observar");
        var estado = radar.Estado == "DadosInsuficientes" ? "DadosInsuficientes"
            : radar.Estado == "Reconectar" || sinaisFragilidade >= 2 ? "Proteger"
            : retomadaEmCurso || sinaisFragilidade == 1 ? "Acompanhar"
            : "Estavel";
        var titulo = estado switch
        {
            "Proteger" => "Proteja a continuidade retomada",
            "Acompanhar" => "Retomada em consolidação",
            "DadosInsuficientes" => "Proteção da retomada em formação",
            _ => "Continuidade consolidada"
        };
        var resumo = estado switch
        {
            "Proteger" => "Há sinais de fragilidade na continuidade. Preserve passos pequenos e evite transformar oscilação em compensação.",
            "Acompanhar" => "A rotina mostra retomada ou pequena oscilação; consolide o básico antes de aumentar exigência.",
            "DadosInsuficientes" => "Ainda faltam registros para diferenciar retomada, descanso planejado e oscilação de rotina.",
            _ => "Sem sinal relevante de nova perda de continuidade nos dados disponíveis."
        };

        return new(estado, titulo, resumo, sinaisFragilidade, retomadaEmCurso, itens,
            "Esta camada não diagnostica recaída, não calcula risco/probabilidade de abandono, não remove XP, não pune streak e não altera treino, nutrição ou medicação automaticamente.");
    }
}
