using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Gera alertas clinico-esportivos transparentes a partir dos indicadores ja explicados pelo painel.
/// Cada alerta expoe os sinais que originaram a leitura nos campos Sinais e Origens; alerta nao e diagnostico, nao calcula probabilidade de lesao
/// e nao prescreve conduta, carga, volume, nutricao ou medicacao automaticamente.
/// </summary>
public static class AlertasClinicoEsportivosService
{
    public static PortalAlertasClinicoEsportivosResponse Montar(PortalPainelMedicinaEsporteResponse painel)
    {
        var porCodigo = painel.Indicadores.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);
        var alertas = new List<PortalAlertaClinicoEsportivoResponse>();

        bool EmAtencao(string codigo)
            => porCodigo.TryGetValue(codigo, out var i) && i.Estado is "Revisar" or "Acompanhar";

        string Sinal(string codigo)
        {
            var i = porCodigo[codigo];
            return $"{i.Titulo}: {i.Estado} • {i.Valor}";
        }

        void Adicionar(string codigo, string nivel, string titulo, string resumo, string acao, params string[] origens)
        {
            var unicas = origens.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            alertas.Add(new(
                codigo, nivel, titulo, resumo,
                unicas.Select(Sinal).ToArray(), unicas, acao));
        }

        // Combinacoes de sinais tem prioridade porque preservam o contexto em vez de transformar um dado isolado em alarme.
        if (EmAtencao("Recuperacao") && EmAtencao("Dor"))
            Adicionar(
                "RecuperacaoDor", "Prioridade", "Recuperacao e dor pedem revisao conjunta",
                "Ha sinais simultaneos em recuperacao e dor. A leitura merece revisao profissional antes de interpretar Performance isoladamente.",
                "Revisar recuperacao, dor e impacto funcional com o profissional; nao inferir diagnostico.",
                "Recuperacao", "Dor");

        if (EmAtencao("Carga") && EmAtencao("Recuperacao"))
            Adicionar(
                "CargaRecuperacao", "Prioridade", "Carga e recuperacao precisam ser contextualizadas",
                "Carga de treino e recuperacao aparecem simultaneamente em acompanhamento/revisao.",
                "Revisar o contexto recente de treino e recuperacao antes de qualquer nova progressao.",
                "Carga", "Recuperacao");

        if (EmAtencao("Dor") && EmAtencao("Prontidao"))
            Adicionar(
                "DorProntidao", "Atencao", "Dor e prontidao merecem acompanhamento",
                "Dor e prontidao do dia estao sinalizando necessidade de acompanhamento no mesmo momento.",
                "Levar os dois sinais para a decisao do dia sem converter a combinacao em diagnostico.",
                "Dor", "Prontidao");

        if (EmAtencao("Progressao") && (EmAtencao("Recuperacao") || EmAtencao("Dor") || EmAtencao("Carga")))
        {
            var origens = new List<string> { "Progressao" };
            foreach (var codigo in new[] { "Recuperacao", "Dor", "Carga" })
                if (EmAtencao(codigo)) origens.Add(codigo);
            Adicionar(
                "ProgressaoContexto", "Prioridade", "Progressao em observacao com sinais concomitantes",
                "Existe progressao supervisionada em observacao junto de sinais que pedem acompanhamento.",
                "Manter supervisao e revisar os sinais de origem antes de considerar nova mudanca.",
                origens.ToArray());
        }

        // Um sinal isolado continua visivel, mas nao ganha gravidade artificial so por existir.
        if (alertas.Count == 0)
        {
            var isolados = painel.Indicadores.Where(x => x.Estado is "Revisar" or "Acompanhar").Take(2).ToArray();
            foreach (var i in isolados)
                Adicionar(
                    $"Isolado{i.Codigo}", i.Estado == "Revisar" ? "Atencao" : "Acompanhamento",
                    $"{i.Titulo} em acompanhamento",
                    "Ha um sinal isolado que merece contexto, sem combinacao suficiente para elevar a prioridade.",
                    "Acompanhar a tendencia e revisar com o profissional se o sinal persistir ou se combinar com outros achados.",
                    i.Codigo);
        }

        var estado = alertas.Any(x => x.Nivel == "Prioridade") ? "Prioridade" :
            alertas.Count > 0 ? "Acompanhar" : "SemAlertas";
        var prioridade = alertas.FirstOrDefault(x => x.Nivel == "Prioridade")
            ?? alertas.FirstOrDefault();

        return new(
            estado,
            "Alertas clinico-esportivos transparentes",
            alertas.Count == 0
                ? "Nenhuma combinacao de sinais exige destaque adicional neste momento. Continue o acompanhamento longitudinal."
                : $"{alertas.Count} alerta(s) explicavel(is) a partir dos dados ja presentes no painel.",
            alertas.Count,
            prioridade?.Titulo ?? "Sem alerta prioritario",
            alertas,
            "Alertas servem para organizar atencao profissional: nao produzir diagnostico, nao estimar risco/probabilidade de lesao e nao realizar prescricao automatica.");
    }
}
