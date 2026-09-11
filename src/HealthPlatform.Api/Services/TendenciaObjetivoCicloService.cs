using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class TendenciaObjetivoCicloService
{
    public static PortalTendenciaObjetivoResponse Montar(
        PortalCicloEsportivoResponse? ciclo,
        PortalMetasCicloResponse metas,
        PortalEvolucaoEsportivaResponse evolucao,
        PortalPerformanceResponse performance,
        PortalCargaTreinoResponse carga,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalAdesaoNutricionalResponse nutricao)
    {
        if (ciclo is null)
            return new("SemCiclo", "SemCiclo", null, "Tendência por objetivo", "Nenhum ciclo ativo para contextualizar a evolução.",
                Array.Empty<PortalTendenciaObjetivoItemResponse>(),
                "A leitura depende do objetivo definido pelo profissional e nunca altera a prescrição automaticamente.");

        var itens = new List<PortalTendenciaObjetivoItemResponse>();
        var perfil = ciclo.PerfilEsportivo?.Trim() ?? "QualidadeDeVida";

        void AddIndicador(string codigo, string eixo, string titulo)
        {
            var i = evolucao.Indicadores.FirstOrDefault(x => x.Codigo == codigo);
            if (i is null) return;
            itens.Add(new(codigo, eixo, NormalizarEstado(i.Estado), titulo, i.Valor,
                string.IsNullOrWhiteSpace(i.Referencia) ? i.Descricao : $"{i.Descricao} Referência: {i.Referencia}."));
        }

        void AddMeta(string codigo, string eixo, string titulo)
        {
            var m = metas.Itens.FirstOrDefault(x => x.Codigo == codigo);
            if (m is null) return;
            var evidencia = m.ProgressoPercentual.HasValue ? $"{m.ValorAtual} • {m.ProgressoPercentual:0}% do alvo" : m.ValorAtual;
            itens.Add(new(codigo, eixo, NormalizarEstadoMeta(m.Estado), titulo, evidencia, m.Descricao));
        }

        switch (perfil)
        {
            case "Hipertrofia":
                AddIndicador("performance", "Performance", "Progressão de performance");
                AddMeta("consistencia", "Adesão", "Consistência do ciclo");
                AddMeta("peso-alvo", "Composição", "Direção do peso-alvo");
                AddIndicador("recuperacao", "Recuperação", "Recuperação para sustentar o ciclo");
                break;
            case "Força":
            case "Performance":
                AddIndicador("performance", "Performance", "Marcas e desempenho");
                AddIndicador("recuperacao", "Recuperação", "Recuperação entre sessões");
                AddIndicador("carga", "Treino", "Carga recente vs. base");
                AddMeta("consistencia", "Adesão", "Consistência do ciclo");
                break;
            case "Emagrecimento":
                AddMeta("peso-alvo", "Composição", "Distância até o peso-alvo");
                AddIndicador("nutricao", "Nutrição", "Adesão alimentar registrada");
                AddMeta("consistencia", "Adesão", "Consistência sustentável");
                AddIndicador("recuperacao", "Recuperação", "Recuperação durante o processo");
                break;
            case "Corrida":
            case "Condicionamento":
                AddMeta("treinos-semana", "Treino", "Frequência semanal planejada");
                AddIndicador("carga", "Treino", "Carga recente vs. base");
                AddIndicador("recuperacao", "Recuperação", "Recuperação entre estímulos");
                AddMeta("consistencia", "Adesão", "Consistência do ciclo");
                break;
            case "QualidadeDeVida":
                AddMeta("consistencia", "Adesão", "Consistência sustentável");
                AddIndicador("recuperacao", "Recuperação", "Recuperação e disposição");
                AddIndicador("hidratacao", "Autocuidado", "Hidratação dentro da meta definida");
                AddIndicador("nutricao", "Nutrição", "Adesão alimentar registrada");
                break;
            default:
                AddMeta("consistencia", "Adesão", "Consistência do ciclo");
                AddIndicador("performance", "Performance", "Performance observada");
                AddIndicador("recuperacao", "Recuperação", "Recuperação recente");
                AddIndicador("carga", "Treino", "Carga recente vs. base");
                break;
        }

        if (itens.Count == 0)
            return new("DadosInsuficientes", perfil, ciclo.Objetivo, "Tendência do objetivo", "O ciclo ainda não possui dados suficientes nos eixos mais relevantes para este objetivo.", itens,
                "A leitura é contextual e descritiva; não converte o objetivo em score clínico nem determina conduta automática.");

        var favoraveis = itens.Count(x => x.Estado == "Favoravel");
        var atencao = itens.Count(x => x.Estado == "Atencao");
        var insuficientes = itens.Count(x => x.Estado == "DadosInsuficientes");
        var estado = atencao >= 2 ? "Revisar" : favoraveis >= 2 ? "Evoluindo" : insuficientes > itens.Count / 2 ? "DadosInsuficientes" : "Estavel";
        var titulo = estado switch
        {
            "Evoluindo" => $"{perfil}: sinais alinhados ao objetivo",
            "Revisar" => $"{perfil}: pontos para revisar",
            "DadosInsuficientes" => $"{perfil}: construindo referência",
            _ => $"{perfil}: tendência estável"
        };
        var resumo = estado switch
        {
            "Evoluindo" => $"{favoraveis} eixo(s) relevante(s) mostram sinais favoráveis dentro do contexto do ciclo.",
            "Revisar" => $"{atencao} eixo(s) relevante(s) pedem revisão de contexto antes de qualquer ajuste.",
            "DadosInsuficientes" => "Continue registrando os eixos ligados ao objetivo para formar uma tendência confiável.",
            _ => "Os eixos mais ligados ao objetivo estão relativamente estáveis no momento."
        };

        return new(estado, perfil, ciclo.Objetivo, titulo, resumo, itens,
            "Objetivos esportivos mudam a interpretação dos dados. Esta leitura não cria score único, não diagnostica e não altera treino, nutrição ou medicação automaticamente.");
    }

    private static string NormalizarEstado(string estado) => estado switch
    {
        "Favoravel" => "Favoravel",
        "Atencao" => "Atencao",
        "DadosInsuficientes" => "DadosInsuficientes",
        _ => "Estavel"
    };

    private static string NormalizarEstadoMeta(string estado) => estado switch
    {
        "Atingida" => "Favoravel",
        "Observar" => "Atencao",
        "DadosInsuficientes" => "DadosInsuficientes",
        _ => "Estavel"
    };
}
