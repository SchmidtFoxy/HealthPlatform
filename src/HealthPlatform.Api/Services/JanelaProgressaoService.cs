using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Explicita as condicoes minimas para considerar progressao depois da reentrada gradual.
/// Nao gera score, nao prescreve progressao e nao aumenta carga/meta automaticamente.
/// </summary>
public static class JanelaProgressaoService
{
    public static PortalJanelaProgressaoResponse Montar(
        PortalReentradaDesafioResponse reentrada,
        PortalEstabilidadeHabitosResponse estabilidade,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse cargaTreino)
    {
        var criterios = new List<PortalCriterioJanelaProgressaoResponse>();

        criterios.Add(new(
            "base-comportamental", "Base comportamental estavel",
            reentrada.ElegivelNovoDesafio,
            reentrada.ElegivelNovoDesafio ? "Atendido" : "Pendente",
            reentrada.ElegivelNovoDesafio
                ? $"{estabilidade.HabitosEstaveis} habito(s) estaveis, sem nova frente automatica."
                : reentrada.Evidencia));

        var semOscilacao = estabilidade.HabitosOscilando == 0;
        criterios.Add(new(
            "sem-oscilacao", "Sem habito oscilando", semOscilacao,
            semOscilacao ? "Atendido" : "Pendente",
            semOscilacao ? "Nenhum eixo comportamental esta oscilando nesta janela." : $"{estabilidade.HabitosOscilando} habito(s) ainda oscilando."));

        var recuperacaoComDados = recuperacao.Tendencia != "DadosInsuficientes";
        var recuperacaoCompativel = recuperacaoComDados && recuperacao.NivelAtencao != "Alta";
        criterios.Add(new(
            "recuperacao", "Recuperacao compativel com progressao", recuperacaoCompativel,
            !recuperacaoComDados ? "SemDados" : recuperacaoCompativel ? "Atendido" : "Revisar",
            !recuperacaoComDados ? "Ainda faltam check-ins para interpretar recuperacao com seguranca." : recuperacao.Mensagem));

        var cargaComDados = cargaTreino.Classificacao != "DadosInsuficientes" && cargaTreino.Classificacao != "ConstruindoBase";
        var cargaCompativel = cargaComDados && cargaTreino.NivelAtencao != "Alta" && cargaTreino.Classificacao != "Revisar";
        criterios.Add(new(
            "carga", "Carga recente sem sinal de revisao", cargaCompativel,
            !cargaComDados ? "SemDados" : cargaCompativel ? "Atendido" : "Revisar",
            !cargaComDados ? "A linha de base de carga ainda esta sendo construida." : cargaTreino.Mensagem));

        var atendidos = criterios.Count(x => x.Atendido);
        var temRevisaoClinica = criterios.Any(x => x.Estado == "Revisar");
        var temSemDados = criterios.Any(x => x.Estado == "SemDados");
        var janelaAberta = reentrada.ElegivelNovoDesafio && atendidos == criterios.Count;

        if (temRevisaoClinica)
            return new("Revisar", "Progressao deve esperar",
                "A base de habitos pode estar melhor, mas recuperacao ou carga ainda pedem revisao antes de qualquer progressao.", false,
                "RevisarContexto", atendidos, criterios.Count, criterios,
                "Mantenha a estrategia atual e revise recuperacao/carga antes de considerar nova exigencia.",
                "Sinal de revisao bloqueia progressao. O sistema nao aumenta carga, volume, meta nutricional ou medicacao automaticamente.");

        if (temSemDados)
            return new("DadosInsuficientes", "Complete a janela de observacao",
                "Existem criterios sem dados suficientes. Isso nao e reprovação nem sinal de baixa adesao.", false,
                "Observar", atendidos, criterios.Count, criterios,
                "Continue registrando a rotina e mantenha o plano atual ate existir contexto suficiente.",
                "Falta de dado nao vira punicao, score ou pressao para treinar mais.");

        if (!reentrada.ElegivelNovoDesafio)
            return new("Aguardar", "A base ainda esta consolidando",
                "A rotina ainda nao abriu espaco para progressao. Manutencao continua sendo um resultado valido.", false,
                "Manter", atendidos, criterios.Count, criterios,
                "Mantenha o foco atual sem adicionar outra meta.",
                "Aguardar nao remove XP, nao pune streak e nao cria nova cobranca automatica.");

        if (janelaAberta)
            return new("JanelaAberta", "Janela de progressao disponivel",
                "Os criterios transparentes desta janela estao satisfeitos para discutir uma unica progressao pequena.", true,
                "ConsiderarProgressao", atendidos, criterios.Count, criterios,
                "Se estiver alinhado ao ciclo e ao profissional, considere uma unica progressao pequena e reavalie a resposta depois.",
                "Janela aberta nao e prescricao: nao cria meta, nao aumenta carga e nao substitui decisao profissional.");

        return new("Aguardar", "Ainda nao e hora de progredir",
            "Nem todos os criterios minimos estao satisfeitos nesta janela.", false,
            "Manter", atendidos, criterios.Count, criterios,
            "Mantenha o plano atual e reavalie quando a base estiver mais consistente.",
            "Nao existe score oculto de prontidao para progressao; os criterios ficam visiveis separadamente.");
    }
}
