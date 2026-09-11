using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Painel de sintese para medicina do esporte. Consolida evidencias existentes sem criar score unico,
/// sem substituir os dados-fonte; este painel deve nao produzir diagnostico, previsao de lesao ou prescricao automatica.
/// Recuperacao e dor tem prioridade de leitura sobre performance isolada.
/// </summary>
public static class PainelMedicinaEsporteService
{
    public static PortalPainelMedicinaEsporteResponse Montar(
        PortalProntidaoDiariaResponse? prontidao,
        PortalDorCorporalResumoResponse dor,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalPerformanceResponse performance,
        PortalAdesaoNutricionalResponse nutricao,
        PortalHidratacaoContextualResponse hidratacao,
        PortalCicloEsportivoResponse? ciclo,
        PortalRegistroProgressaoResponse progressao,
        PortalPerfilRespostaAtletaResponse perfil)
    {
        var indicadores = new List<PortalPainelMedicinaEsporteIndicadorResponse>();

        indicadores.Add(new(
            "Recuperacao", "Recuperacao", EstadoAtencao(recuperacao.NivelAtencao),
            "Recuperacao recente",
            recuperacao.Tendencia,
            $"{recuperacao.DiasObservados} dia(s) observados; prontidao media 7d: {Fmt(recuperacao.ProntidaoMedia7)}.",
            "ClinicaEsportiva"));

        var estadoDor = dor.Registros7 == 0 ? "SemDados" : dor.IntensidadeMaxima7 >= 7 || dor.ImpactoMaximoTreino7 >= 7 ? "Revisar" : dor.IntensidadeMaxima7 >= 4 || dor.ImpactoMaximoTreino7 >= 4 ? "Acompanhar" : "Estavel";
        indicadores.Add(new(
            "Dor", "Dor", estadoDor,
            "Dor e impacto funcional",
            dor.Registros7 == 0 ? "Sem registro recente" : $"Max {dor.IntensidadeMaxima7}/10 • impacto {dor.ImpactoMaximoTreino7}/10",
            $"{dor.RegioesAtivas} regiao(oes) ativa(s) nos ultimos 7 dias.",
            "ClinicaEsportiva"));

        var estadoProntidao = prontidao is null ? "SemDados" :
            string.Equals(prontidao.RecomendacaoTreino, "Recovery", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(prontidao.RecomendacaoTreino, "Light", StringComparison.OrdinalIgnoreCase) ? "Acompanhar" : "Estavel";
        indicadores.Add(new(
            "Prontidao", "Prontidao", estadoProntidao,
            "Prontidao do dia",
            prontidao is null ? "Sem check-in hoje" : $"{prontidao.Score}/100 • {prontidao.RecomendacaoTreino}",
            prontidao?.MotivoRecomendacao ?? "Sem leitura diaria suficiente.",
            "DiaAtual"));

        indicadores.Add(new(
            "Carga", "Treinamento", EstadoAtencao(carga.NivelAtencao),
            "Carga de treino",
            carga.Classificacao,
            $"Carga interna 7d: {Fmt(carga.CargaInterna7)}; relacao com base: {Fmt(carga.RelacaoCargaComBase)}.",
            "Treinamento"));

        indicadores.Add(new(
            "Performance", "Performance", "Contexto",
            "Performance recente",
            performance.Tendencia,
            $"{performance.TreinosPeriodo} treino(s), {performance.PrsRecentes} PR(s) recente(s). Performance nao prevalece sobre recuperacao ou dor.",
            "Performance"));

        indicadores.Add(new(
            "Nutricao", "Adesao", EstadoContextual(nutricao.Estado),
            "Adesao nutricional",
            nutricao.Estado,
            nutricao.Mensagem,
            "Comportamento"));

        indicadores.Add(new(
            "Hidratacao", "Adesao", EstadoAtencao(hidratacao.NivelAtencao),
            "Hidratacao",
            hidratacao.Estado,
            hidratacao.Mensagem,
            "Comportamento"));

        indicadores.Add(new(
            "Progressao", "Progressao", progressao.PossuiEventoAtivo ? "Acompanhar" : "Estavel",
            "Progressao supervisionada",
            progressao.PossuiEventoAtivo ? "Em observacao" : "Sem evento ativo",
            progressao.Resumo,
            "Supervisao"));

        var atencao = indicadores.Count(x => x.Estado is "Revisar" or "Acompanhar");
        var estaveis = indicadores.Count(x => x.Estado == "Estavel");
        var prioridade = indicadores.FirstOrDefault(x => x.Estado == "Revisar" && x.Categoria is "Recuperacao" or "Dor")
            ?? indicadores.FirstOrDefault(x => x.Estado == "Revisar")
            ?? indicadores.FirstOrDefault(x => x.Estado == "Acompanhar" && x.Categoria is "Recuperacao" or "Dor")
            ?? indicadores.FirstOrDefault(x => x.Estado == "Acompanhar");

        var estado = indicadores.Any(x => x.Estado == "Revisar") ? "Revisar" :
            indicadores.Any(x => x.Estado == "Acompanhar") ? "Acompanhar" :
            indicadores.Count(x => x.Estado == "SemDados") >= 3 ? "DadosInsuficientes" : "Estavel";

        var contextos = new List<string>
        {
            ciclo is null ? "Sem ciclo esportivo ativo." : $"Ciclo: {ciclo.Nome} • semana {ciclo.SemanaAtual}/{ciclo.TotalSemanas} • objetivo: {ciclo.Objetivo ?? "nao informado"}.",
            $"Perfil longitudinal: {perfil.Estado}; {perfil.EixosComHistorico} eixo(s) com historico.",
            "Recuperacao e dor tem prioridade de leitura sobre performance isolada.",
            "Cada indicador preserva sua evidencia de origem; o painel nao cria score unico."
        };

        return new(
            estado,
            "Painel de medicina do esporte",
            $"Sintese profissional de {indicadores.Count} eixo(s): {atencao} em acompanhamento/revisao e {estaveis} estavel(is).",
            atencao,
            estaveis,
            prioridade?.Titulo ?? "Manter acompanhamento longitudinal",
            indicadores,
            contextos,
            "Painel de sintese nao e diagnostico, previsao de lesao ou prescricao. Decisoes clinicas e de treinamento permanecem supervisionadas e apoiadas nos dados-fonte.");
    }

    private static string EstadoAtencao(string? nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel)) return "SemDados";
        if (nivel.Contains("Alta", StringComparison.OrdinalIgnoreCase) || nivel.Contains("Revis", StringComparison.OrdinalIgnoreCase) || nivel.Contains("Crit", StringComparison.OrdinalIgnoreCase)) return "Revisar";
        if (nivel.Contains("Moder", StringComparison.OrdinalIgnoreCase) || nivel.Contains("Aten", StringComparison.OrdinalIgnoreCase)) return "Acompanhar";
        return "Estavel";
    }

    private static string EstadoContextual(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado) || estado.Contains("Dados", StringComparison.OrdinalIgnoreCase)) return "SemDados";
        if (estado.Contains("Aten", StringComparison.OrdinalIgnoreCase) || estado.Contains("Baix", StringComparison.OrdinalIgnoreCase) || estado.Contains("Revis", StringComparison.OrdinalIgnoreCase)) return "Acompanhar";
        return "Estavel";
    }

    private static string Fmt(decimal? valor) => valor.HasValue ? valor.Value.ToString("0.##") : "sem dado";
}
