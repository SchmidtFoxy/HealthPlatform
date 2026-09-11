using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Camada de sintese explicavel. Nao diagnostica, nao prescreve e nao altera o plano.
/// Prioriza sinais ja calculados pelos motores esportivos do portal.
/// </summary>
public static class CoachDiarioService
{
    public static PortalCoachDiarioResponse Montar(
        PortalProntidaoDiariaResponse? prontidao,
        PortalDorCorporalResumoResponse dorCorporal,
        PortalEstrategiaDiaResponse estrategia,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalPerformanceResponse performance,
        PortalExecucaoDiaResponse execucao,
        PortalCicloEsportivoResponse? ciclo)
    {
        var prioridades = new List<PortalCoachPrioridadeResponse>();

        void Adicionar(string codigo, string categoria, string nivel, string titulo, string motivo, string acao)
        {
            if (prioridades.Any(x => x.Codigo == codigo)) return;
            prioridades.Add(new(codigo, categoria, nivel, titulo, motivo, acao));
        }

        if (prontidao is null)
        {
            Adicionar(
                "checkin-prontidao", "Autocuidado", "Alta",
                "Faça seu check-in antes de decidir a intensidade",
                "Ainda não há prontidão registrada para hoje.",
                "Registre sono, energia, dor, disposição e recuperação; a estratégia do dia será recalculada sem alterar sua prescrição.");
        }

        var recuperacaoPedeAtencao =
            recuperacao.Tendencia is "Atencao" or "Observar" ||
            prontidao?.RecomendacaoTreino == "Recuperacao" ||
            prontidao?.DorNivel >= 6 ||
            prontidao?.RecuperacaoNivel <= 4;

        if (dorCorporal.NivelAtencao == "Alta")
        {
            var principal = dorCorporal.RegistrosRecentes.OrderByDescending(x => Math.Max(x.Intensidade, x.ImpactoTreino)).FirstOrDefault();
            Adicionar(
                "dor-localizada-alta", "Dor", "Alta",
                principal is null ? "Dor localizada merece revisão" : $"Atenção à região: {principal.Regiao}{(string.IsNullOrWhiteSpace(principal.Lado) ? "" : $" ({principal.Lado})")}",
                principal is null ? dorCorporal.Mensagem : $"Intensidade {principal.Intensidade}/10 e impacto no treino {principal.ImpactoTreino}/10.",
                "Não use XP, PR ou meta semanal como motivo para forçar a região; ajuste a execução dentro do plano e procure avaliação profissional se o sinal persistir ou piorar.");
        }
        else if (dorCorporal.NivelAtencao == "Media")
        {
            Adicionar(
                "acompanhar-dor-localizada", "Dor", "Media",
                "Acompanhe o desconforto localizado",
                dorCorporal.Mensagem,
                "Observe a resposta ao treino e registre novamente se intensidade ou impacto mudarem.");
        }

        if (recuperacaoPedeAtencao)
        {
            var detalhes = new List<string>();
            if (prontidao is not null) detalhes.Add($"prontidão {prontidao.Score}/100");
            if (recuperacao.SonoMedio7.HasValue) detalhes.Add($"sono médio {recuperacao.SonoMedio7:0.0}h");
            if (recuperacao.DorMedia7.HasValue) detalhes.Add($"dor média {recuperacao.DorMedia7:0.0}/10");
            if (recuperacao.RecuperacaoMedia7.HasValue) detalhes.Add($"recuperação média {recuperacao.RecuperacaoMedia7:0.0}/10");

            Adicionar(
                "priorizar-recuperacao", "Recuperação", "Alta",
                "Priorize recuperação e qualidade hoje",
                detalhes.Count > 0 ? $"Os sinais recentes pedem cautela: {string.Join(", ", detalhes)}." : recuperacao.Mensagem,
                $"Siga a estratégia {estrategia.IntensidadeSugerida} já definida para hoje; não aumente carga ou volume automaticamente.");
        }

        if (carga.Classificacao == "Revisar")
        {
            var relacao = carga.RelacaoCargaComBase.HasValue ? $"{carga.RelacaoCargaComBase:0.00}× a sua base" : "acima do padrão recente";
            Adicionar(
                "revisar-carga", "Carga", "Alta",
                "Não progrida carga antes de revisar a semana",
                $"A carga recente está {relacao} e o motor encontrou contexto que merece revisão.",
                "Mantenha a prescrição vigente e leve o contexto ao profissional antes de acrescentar volume, intensidade ou sessões.");
        }
        else if (carga.Classificacao == "AcimaDaBase")
        {
            Adicionar(
                "consolidar-carga", "Carga", "Media",
                "Consolide a carga atual",
                $"A semana está em {carga.RelacaoCargaComBase:0.00}× a linha de base.",
                "Priorize técnica, recuperação e execução do plano; mais carga não é necessária só porque a semana está indo bem.");
        }

        if (execucao.ProgressoPercentual < 100m)
        {
            var pendentes = execucao.Itens.Where(x => x.Status != "Concluido").ToList();
            var primeiro = pendentes.FirstOrDefault(x => x.Obrigatorio) ?? pendentes.FirstOrDefault();
            if (primeiro is not null)
            {
                Adicionar(
                    "proxima-acao", "Execução", "Media",
                    $"Próxima ação: {primeiro.Titulo}",
                    $"Seu roteiro está {execucao.ProgressoPercentual:0}% concluído.",
                    primeiro.Descricao);
            }
        }
        else
        {
            Adicionar(
                "dia-consolidado", "Consistência", "Baixa",
                "Dia bem executado: preserve a consistência",
                "Os itens acompanhados do roteiro de hoje estão concluídos.",
                "Evite adicionar esforço só para buscar mais XP; use o restante do dia para recuperação e rotina.");
        }

        if (performance.PrsRecentes > 0)
        {
            Adicionar(
                "pr-recente", "Performance", "Baixa",
                "Use o PR como evidência, não como obrigação",
                $"Há {performance.PrsRecentes} PR recente(s) no histórico.",
                "Mantenha progressão dentro do ciclo; não tente repetir ou superar uma melhor marca em todo treino.");
        }

        if (ciclo is not null && ciclo.MetaTreinosSemanais.HasValue)
        {
            Adicionar(
                "meta-ciclo", "Ciclo", "Baixa",
                $"Mantenha o foco do ciclo: {ciclo.Nome}",
                $"Semana {ciclo.SemanaAtual}/{ciclo.TotalSemanas}; meta de {ciclo.MetaTreinosSemanais} treino(s) por semana.",
                "Use as decisões de hoje para sustentar o ciclo inteiro, não para maximizar um único dia.");
        }

        var ordenadas = prioridades.OrderBy(x => Peso(x.Nivel)).ThenBy(x => x.Categoria).Take(3).ToList();
        var estado = recuperacaoPedeAtencao || carga.Classificacao == "Revisar"
            ? "Recuperar"
            : execucao.ProgressoPercentual >= 100m ? "Consolidar" : "Executar";

        var titulo = estado switch
        {
            "Recuperar" => "Hoje, o melhor progresso é respeitar os sinais",
            "Consolidar" => "Bom trabalho: transforme um bom dia em consistência",
            _ => "Execute o plano certo para hoje"
        };
        var resumo = ordenadas.Count == 0
            ? "Mantenha o plano definido pelo profissional e registre como seu corpo responde."
            : ordenadas[0].Motivo;

        return new(
            estado, titulo, resumo,
            "O Coach Diário organiza dados já registrados; não diagnostica, não substitui avaliação profissional e não altera a prescrição automaticamente.",
            ordenadas);
    }

    private static int Peso(string nivel) => nivel switch { "Alta" => 0, "Media" => 1, _ => 2 };
}
