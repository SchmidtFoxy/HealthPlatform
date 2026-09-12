using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class RelatorioEsportivoProfissionalService
{
    public static PortalRelatorioEsportivoProfissionalResponse Montar(
        DateOnly dia,
        PortalCicloEsportivoResponse? ciclo,
        PortalPainelMedicinaEsporteResponse painel,
        PortalAlertasClinicoEsportivosResponse alertas,
        PortalMapaCorporalLongitudinalResponse mapaCorporal,
        PortalCargaIndividualizadaResponse cargaIndividualizada,
        PortalBlocoTreinamentoResponse blocoTreinamento,
        PortalDeloadRecuperacaoPlanejadaResponse deload,
        PortalReadinessContextualTreinoResponse readiness,
        PortalSessaoPlanejadaExecutadaResponse sessao,
        PortalRespostaSessaoResponse respostaSessao,
        PortalRegistroProgressaoResponse registroProgressao,
        PortalRetornoGradualResponse retornoGradual,
        PortalPerfilRespostaAtletaResponse perfilResposta)
    {
        // Relatorio profissional consolida evidencias ja calculadas; cada secao permanece rastreavel a sua origem.
        // prioridade clinico-esportiva: dor, recuperacao e alertas de revisao prevalecem sobre performance favoravel isolada.
        // nao produzir diagnostico, nao estimar risco de lesao e nao gerar prescricao automatica.
        // o relatorio nao cria score geral do atleta e nao transforma associacao temporal em causalidade.
        var secoes = new List<PortalRelatorioEsportivoSecaoResponse>();

        var periodo = ciclo is null
            ? $"Referencia em {dia:dd/MM/yyyy}"
            : $"{ciclo.DataInicio:dd/MM/yyyy} a {ciclo.DataFim:dd/MM/yyyy} • semana {ciclo.SemanaAtual}/{ciclo.TotalSemanas}";

        var blocoEstado = blocoTreinamento.Estado;
        secoes.Add(new(
            "ciclo-bloco",
            "Planejamento",
            blocoEstado,
            "Ciclo e bloco atual",
            ciclo is null
                ? "Sem ciclo esportivo ativo; o relatório preserva os demais dados observacionais sem inventar um bloco."
                : $"{ciclo.Nome} • {ciclo.PerfilEsportivo} • {ciclo.Objetivo ?? "objetivo nao informado"} • bloco {blocoTreinamento.Nome ?? "nao configurado"}.",
            new[]
            {
                $"Ciclo: {(ciclo?.Status ?? "SemCiclo")}",
                $"Bloco: {blocoTreinamento.Estado}",
                $"Semana do bloco: {(blocoTreinamento.SemanaAtual?.ToString() ?? "—")}/{(blocoTreinamento.TotalSemanas?.ToString() ?? "—")}",
                $"Deload/recuperacao planejada: {(deload.IntencaoPlanejada ? deload.TipoIntencao : "nao")}"
            }));

        var cargaAtencao = cargaIndividualizada.Estado is "RevisarContexto" or "ObservarContexto";
        var readinessAtencao = readiness.Estado is "RevisarAntesDaSessao" or "AdaptarAoContexto";
        var respostaAtencao = respostaSessao.Estado is "Revisar" or "Observar";
        var estadoCargaRecuperacao = cargaAtencao || readinessAtencao || respostaAtencao ? "Revisar" : "Acompanhar";
        secoes.Add(new(
            "carga-recuperacao",
            "Carga e recuperacao",
            estadoCargaRecuperacao,
            "Carga individual e resposta",
            $"Carga: {cargaIndividualizada.PosicaoHistorica} • readiness: {readiness.Estado} • resposta a sessao: {respostaSessao.Estado}.",
            new[]
            {
                $"Carga 7d: {(cargaIndividualizada.CargaAtual7.HasValue ? cargaIndividualizada.CargaAtual7.Value.ToString("0.#") : "sem dado")}",
                $"Referencia individual: {cargaIndividualizada.Estado}",
                $"Readiness contextual: {readiness.Estado}",
                $"Resposta a sessao: {respostaSessao.Estado}"
            }));

        var regioesAcompanhar = mapaCorporal.Regioes.Where(x => x.Estado == "Acompanhar" || x.Tendencia == "MaisPresente").ToList();
        var estadoDor = regioesAcompanhar.Count > 0 ? "Revisar" : mapaCorporal.TotalRegistros > 0 ? "Acompanhar" : "SemDados";
        secoes.Add(new(
            "dor-mapa",
            "Dor e funcao",
            estadoDor,
            "Mapa corporal longitudinal",
            regioesAcompanhar.Count > 0
                ? $"{regioesAcompanhar.Count} regiao(oes) merecem acompanhamento pela recorrencia, intensidade, impacto ou aumento de presenca."
                : mapaCorporal.TotalRegistros > 0
                    ? "Ha registros corporais no periodo sem destaque longitudinal adicional neste resumo."
                    : "Sem registros de dor localizada no periodo observado.",
            mapaCorporal.Regioes.Take(5)
                .Select(x => $"{x.Regiao}{(string.IsNullOrWhiteSpace(x.Lado) ? "" : $" ({x.Lado})")}: {x.Estado} • tendencia {x.Tendencia} • max {x.IntensidadeMaxima}/10")
                .ToArray()));

        var estadoSessao = sessao.Estado is "AcimaDoPlanejado" ? "Acompanhar" :
                           sessao.Estado is "Adaptada" or "AdaptadaAoContexto" ? "Contextual" :
                           sessao.Estado;
        secoes.Add(new(
            "sessao-resposta",
            "Sessao",
            estadoSessao,
            "Planejado, executado e resposta",
            $"{sessao.Estado} • resposta posterior {respostaSessao.Estado}.",
            new[]
            {
                $"Sessao planejada: {sessao.SessaoPlanejada ?? "sem sessao"}",
                $"Sessao executada: {sessao.SessaoExecutada ?? "sem execucao"}",
                $"RPE executado: {(sessao.RpeExecutado?.ToString() ?? "—")}",
                $"Resposta posterior: {respostaSessao.Resumo}"
            }));

        var progressaoEstado = registroProgressao.PossuiEventoAtivo ? "EmObservacao" : "SemEventoAtivo";
        secoes.Add(new(
            "progressao-retorno",
            "Progressao e retorno",
            retornoGradual.Estado == "RevisarAntesRetorno" ? "Revisar" : progressaoEstado,
            "Progressao supervisionada e retorno",
            $"Progressao: {registroProgressao.Estado} • retorno: {retornoGradual.Estado}.",
            new[]
            {
                $"Evento de progressao ativo: {(registroProgressao.PossuiEventoAtivo ? "sim" : "nao")}",
                $"Retorno gradual: {retornoGradual.Estado}",
                $"Pode avancar etapa de retorno: {(retornoGradual.PodeAvancarEtapa ? "sim, para discussao supervisionada" : "nao")}",
                $"Perfil longitudinal: {perfilResposta.Estado}"
            }));

        var indicadoresAdesao = painel.Indicadores
            .Where(x => x.Categoria is "Nutricao" or "Hidratacao")
            .ToList();
        secoes.Add(new(
            "adesao-suporte",
            "Suporte diario",
            indicadoresAdesao.Any(x => x.Prioridade == "Alta" || x.Estado.Contains("Revis", StringComparison.OrdinalIgnoreCase)) ? "Revisar" : "Acompanhar",
            "Nutrição, hidratação e suporte da rotina",
            indicadoresAdesao.Count == 0
                ? "Sem indicadores adicionais de nutrição/hidratação disponíveis neste painel."
                : string.Join(" • ", indicadoresAdesao.Select(x => $"{x.Titulo}: {x.Estado}")),
            indicadoresAdesao.Select(x => $"{x.Titulo}: {x.Valor} • {x.Contexto}").ToArray()));

        var pontosAtencao = new List<string>();
        foreach (var alerta in alertas.Alertas.Where(x => x.Nivel is "Prioridade" or "Acompanhar").Take(5))
            pontosAtencao.Add($"{alerta.Titulo}: {alerta.Resumo}");
        foreach (var secao in secoes.Where(x => x.Estado == "Revisar"))
            if (!pontosAtencao.Any(x => x.StartsWith(secao.Titulo, StringComparison.OrdinalIgnoreCase)))
                pontosAtencao.Add($"{secao.Titulo}: {secao.Resumo}");

        var pontosEstaveis = secoes
            .Where(x => x.Estado is not "Revisar" and not "SemDados")
            .Take(4)
            .Select(x => $"{x.Titulo}: {x.Resumo}")
            .ToList();

        var estado = pontosAtencao.Count > 0 ? "Revisar" :
                     painel.Estado == "DadosInsuficientes" ? "DadosInsuficientes" :
                     "Acompanhar";
        var prioridade = pontosAtencao.FirstOrDefault() ?? painel.PrioridadePrincipal;
        var resumo = estado switch
        {
            "Revisar" => "O resumo profissional encontrou evidencias que merecem revisao contextual antes de novas decisoes esportivas.",
            "DadosInsuficientes" => "O relatorio foi montado, mas alguns eixos ainda nao possuem dados suficientes para uma leitura longitudinal completa.",
            _ => "Os principais eixos esportivos estao organizados para revisao longitudinal, preservando cada fonte de evidencia separadamente."
        };

        const string seguranca =
            "Relatorio esportivo profissional e uma sintese rastreavel dos dados registrados: nao produzir diagnostico, nao estimar risco de lesao, nao gerar prescricao automatica, nao criar score geral do atleta e nao provar causalidade. Decisoes clinicas e de treinamento permanecem sob julgamento profissional.";

        return new(
            estado,
            "Relatorio esportivo profissional",
            dia,
            periodo,
            resumo,
            prioridade,
            ciclo?.Nome,
            ciclo?.Objetivo,
            secoes,
            pontosAtencao.Take(6).ToArray(),
            pontosEstaveis.ToArray(),
            seguranca);
    }
}
