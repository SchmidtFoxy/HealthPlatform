using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Organiza uma progressao como discussao supervisionada. Nao prescreve nem executa mudancas.
/// </summary>
public static class PlanoProgressaoSupervisionadaService
{
    public static PortalPlanoProgressaoSupervisionadaResponse Montar(
        PortalDecisaoProgressaoResponse decisao,
        PortalJanelaProgressaoResponse janela)
    {
        if (!janela.JanelaAberta || !decisao.PodeConsiderarProgressao || string.IsNullOrWhiteSpace(decisao.CategoriaSugerida))
        {
            var estado = janela.Estado == "DadosInsuficientes" ? "DadosInsuficientes" : janela.Estado == "Revisar" ? "Revisar" : "Manter";
            var criteriosPendentes = janela.Criterios
                .Where(x => !x.Atendido)
                .Select(x => new PortalCriterioPlanoProgressaoResponse(x.Codigo, x.Titulo, x.Estado, x.Evidencia))
                .ToList();

            return new(estado, "Progressao permanece em observacao",
                "O plano supervisionado respeita a janela anterior e nao abre uma nova exigencia enquanto houver criterio pendente.",
                false, null, "NaoAbrirPlano", janela.Resumo,
                janela.Acao, criteriosPendentes, "Reavaliar quando os criterios pendentes tiverem novo contexto.",
                "Plano supervisionado nao executa progressao. Falta de dado, recuperacao ou carga em revisao nunca viram pressao para avancar.");
        }

        var eixo = decisao.CategoriaSugerida;
        var criterios = new List<PortalCriterioPlanoProgressaoResponse>
        {
            new("janela-aberta", "Janela de progressao aberta", "Atendido",
                $"{janela.CriteriosAtendidos}/{janela.CriteriosTotais} criterios transparentes estao atendidos."),
            new("eixo-definido", "Um unico eixo em discussao", "Atendido",
                $"A decisao assistida priorizou {eixo}; os demais eixos devem permanecer estaveis."),
            new("observar-resposta", "Observar resposta antes de nova mudanca", "Acompanhar",
                "Qualquer mudanca deve ser revisada antes de considerar outra progressao.")
        };

        return new("Supervisionar", "Plano de progressao supervisionada",
            "Existe contexto para discutir uma mudanca pequena, com observacao da resposta antes de qualquer novo avanco.",
            true, eixo, "DiscutirComProfissional", decisao.Evidencia,
            $"Discuta {eixo} com o profissional, preserve os outros eixos e faca uma mudanca por vez.",
            criterios, "Reavaliar apos observar a resposta, sem prazo automatico imposto pelo sistema.",
            "Plano supervisionado nao e prescricao: nao define carga, volume, calorias, medicacao e nao cria meta automatica.");
    }
}
