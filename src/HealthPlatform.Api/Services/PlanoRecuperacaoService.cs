using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Sintese contextual de recuperacao. Usa sinais ja calculados e nunca diagnostica,
/// prescreve tratamento ou altera automaticamente o plano profissional.
/// </summary>
public static class PlanoRecuperacaoService
{
    public static PortalPlanoRecuperacaoResponse Montar(
        PortalProntidaoDiariaResponse? prontidao,
        PortalDorCorporalResumoResponse dorCorporal,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalEstrategiaDiaResponse estrategia,
        PortalExecucaoDiaResponse execucao)
    {
        var itens = new List<PortalPlanoRecuperacaoItemResponse>();

        void Adicionar(string codigo, string categoria, string prioridade, string titulo, string orientacao)
        {
            if (itens.Any(x => x.Codigo == codigo)) return;
            itens.Add(new(codigo, categoria, prioridade, titulo, orientacao));
        }

        if (prontidao is null)
        {
            Adicionar("checkin", "Monitoramento", "Alta", "Comece pelo check-in de prontidão",
                "Registre sono, energia, dor, disposição e recuperação antes de decidir como executar o treino de hoje.");
        }

        var dorAlta = dorCorporal.NivelAtencao == "Alta";
        if (dorAlta)
        {
            var principal = dorCorporal.RegistrosRecentes
                .OrderByDescending(x => Math.Max(x.Intensidade, x.ImpactoTreino))
                .FirstOrDefault();
            var regiao = principal is null ? "região com maior desconforto" :
                $"{principal.Regiao}{(string.IsNullOrWhiteSpace(principal.Lado) ? "" : $" ({principal.Lado})")}";
            Adicionar("proteger-regiao", "Dor", "Alta", $"Proteja a {regiao}",
                "Evite usar meta, XP ou PR como motivo para forçar a região. Respeite os limites do plano e procure avaliação profissional se houver persistência, piora ou incapacidade funcional.");
        }
        else if (dorCorporal.NivelAtencao == "Media")
        {
            Adicionar("monitorar-regiao", "Dor", "Media", "Monitore o desconforto localizado",
                "Observe como a região responde às atividades e registre novamente se intensidade ou impacto no treino mudarem.");
        }

        var recuperacaoBaixa = recuperacao.Tendencia is "Atencao" or "Observar" ||
            prontidao?.RecomendacaoTreino == "Recuperacao" || prontidao?.RecuperacaoNivel <= 4;
        if (recuperacaoBaixa)
        {
            Adicionar("recuperacao-base", "Recuperação", "Alta", "Dê prioridade à recuperação hoje",
                $"Siga a estratégia {estrategia.IntensidadeSugerida} já definida e evite acrescentar carga ou volume fora da prescrição.");
        }

        if (recuperacao.SonoMedio7.HasValue && recuperacao.SonoMedio7.Value < 6.5m)
        {
            Adicionar("sono", "Sono", "Media", "Proteja sua janela de sono",
                $"Sua média recente está em {recuperacao.SonoMedio7.Value:0.0}h. Priorize rotina e oportunidade de sono sem usar treino extra para compensar o dia.");
        }

        if (carga.Classificacao == "Revisar")
        {
            Adicionar("carga", "Carga", "Alta", "Consolide antes de progredir",
                "A carga recente e os sinais de recuperação pedem revisão. Mantenha a prescrição vigente e não acrescente sessões, volume ou intensidade por conta própria.");
        }
        else if (carga.Classificacao == "AcimaDaBase")
        {
            Adicionar("carga", "Carga", "Media", "Consolide a carga da semana",
                "A semana está acima da sua linha de base recente. Priorize execução técnica, alimentação, hidratação e recuperação antes de buscar mais carga.");
        }

        if (!string.IsNullOrWhiteSpace(estrategia.FocoHidratacao))
        {
            Adicionar("hidratacao", "Hidratação", "Baixa", "Mantenha a hidratação do plano",
                estrategia.FocoHidratacao);
        }

        if (execucao.ProgressoPercentual >= 100m)
        {
            Adicionar("encerrar-dia", "Rotina", "Baixa", "O trabalho do dia já foi feito",
                "Evite adicionar esforço apenas para ganhar XP. Use o restante do dia para alimentação, hidratação, mobilidade confortável e sono conforme seu plano.");
        }

        var ordenados = itens.OrderBy(x => Peso(x.Prioridade)).ThenBy(x => x.Categoria).Take(4).ToList();
        var estado = dorAlta ? "Protecao" : recuperacaoBaixa || carga.Classificacao == "Revisar" ? "Recuperacao" : "Equilibrio";
        var foco = ordenados.FirstOrDefault()?.Titulo ?? "Sustente a rotina de recuperação";
        var resumo = estado switch
        {
            "Protecao" => "Há um sinal localizado relevante: hoje, proteger a região é parte do progresso.",
            "Recuperacao" => "Os sinais recentes favorecem uma execução conservadora e foco em recuperação.",
            _ => "Os sinais estão compatíveis com manutenção da rotina e recuperação planejada."
        };

        return new PortalPlanoRecuperacaoResponse(
            estado, foco, resumo, ordenados,
            "Este plano organiza sinais de autocuidado; não diagnostica lesão, não prescreve tratamento e não altera automaticamente treino, nutrição ou medicação.");
    }

    private static int Peso(string prioridade) => prioridade switch { "Alta" => 0, "Media" => 1, _ => 2 };
}
