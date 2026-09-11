using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Consolida um perfil longitudinal descritivo do proprio atleta a partir de historico realmente registrado,
/// resposta longitudinal atual e estabilidade de habitos. O perfil e revisavel: nao rotula o atleta,
/// nao calcula score de responsividade e nao prescreve progressao, carga, dieta ou conduta clinica.
/// </summary>
public static class PerfilRespostaAtletaService
{
    public static PortalPerfilRespostaAtletaResponse Montar(
        PortalToleranciaProgressaoResponse tolerancia,
        PortalInterpretacaoLongitudinalProgressaoResponse longitudinal,
        PortalEstabilidadeHabitosResponse habitos,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalPerformanceResponse performance)
    {
        var eixos = tolerancia.Eixos.Select(x => MontarEixo(x, longitudinal)).ToList();

        if (eixos.Count == 0)
        {
            return new(
                "DadosInsuficientes",
                "Perfil de resposta do atleta ainda em formacao",
                "Ainda faltam eventos repetidos no mesmo eixo para consolidar um retrato longitudinal individual.",
                0,
                habitos.HabitosEstaveis,
                habitos.HabitosOscilando,
                eixos,
                "O sistema preserva o contexto atual, mas nao transforma poucos registros em um perfil fixo.",
                "Perfil descritivo nao e diagnostico, score de responsividade, previsao de lesao ou autorizacao automatica para progredir.");
        }

        var requerContexto = eixos.Any(x => x.PadraoHistorico == "RequerContexto") ||
            string.Equals(longitudinal.Estado, "Atencao", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(longitudinal.Estado, "Misto", StringComparison.OrdinalIgnoreCase);
        var variavel = eixos.Any(x => x.PadraoHistorico == "Variavel");
        var estavel = eixos.All(x => x.PadraoHistorico == "EstabilidadeRecorrente");

        var estado = requerContexto ? "ContextoSensivel" : variavel ? "PerfilVariavel" : estavel ? "EstabilidadeRecorrente" : "PerfilDescritivo";
        var leitura = estado switch
        {
            "ContextoSensivel" => "O historico individual apresenta sinais que pedem interpretacao de contexto antes de qualquer nova mudanca.",
            "PerfilVariavel" => "A resposta muda entre eventos; o momento do ciclo e o contexto de recuperacao continuam centrais.",
            "EstabilidadeRecorrente" => "Ha recorrencia de respostas estaveis/favoraveis nos eixos observados, sem converter isso em permissao automatica para aumentar exigencia.",
            _ => "O perfil resume recorrencias do proprio atleta mantendo cada evidencia separada e revisavel."
        };

        var contextoAtual = new List<string>();
        contextoAtual.Add($"Habitos: {habitos.HabitosEstaveis} estavel(is), {habitos.HabitosOscilando} oscilando.");
        contextoAtual.Add($"Recuperacao atual: {recuperacao.Tendencia} / atencao {recuperacao.NivelAtencao}.");
        contextoAtual.Add($"Carga atual: {carga.Classificacao} / atencao {carga.NivelAtencao}.");
        contextoAtual.Add($"Performance atual: {performance.Tendencia}.");

        return new(
            estado,
            "Perfil de resposta do atleta",
            $"{eixos.Count} eixo(s) possuem historico suficiente para um retrato longitudinal individual e revisavel.",
            eixos.Count,
            habitos.HabitosEstaveis,
            habitos.HabitosOscilando,
            eixos,
            leitura + " " + string.Join(" ", contextoAtual),
            "O perfil descreve padroes observados; nao classifica o atleta para sempre, nao calcula score de responsividade e nao prescreve nova progressao automaticamente.");
    }

    private static PortalPerfilRespostaAtletaEixoResponse MontarEixo(
        PortalToleranciaProgressaoEixoResponse eixo,
        PortalInterpretacaoLongitudinalProgressaoResponse longitudinal)
    {
        var correspondeAtual = !string.IsNullOrWhiteSpace(longitudinal.EixoProgressao) &&
            string.Equals(longitudinal.EixoProgressao, eixo.Eixo, StringComparison.OrdinalIgnoreCase);

        var estadoAtual = correspondeAtual ? longitudinal.Estado : "SemEventoAtualComparavel";
        var contexto = correspondeAtual
            ? "Existe uma leitura longitudinal atual para este mesmo eixo."
            : "O padrao historico permanece separado da resposta do evento atual.";

        var evidencias = eixo.Evidencias.ToList();
        evidencias.Add($"Padrao historico: {eixo.Padrao}.");
        evidencias.Add($"Estado longitudinal atual: {estadoAtual}.");

        return new(
            eixo.Eixo,
            eixo.Padrao,
            eixo.TotalEventos,
            eixo.EventosInterpretaveis,
            estadoAtual,
            contexto,
            evidencias);
    }
}
