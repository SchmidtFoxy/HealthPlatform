using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Organiza qual eixo pode ser discutido primeiro quando a janela de progressao ja esta aberta.
/// E decisao assistida, nao prescricao: nao cria meta, nao define carga e nao executa progressao automaticamente.
/// </summary>
public static class DecisaoProgressaoService
{
    public static PortalDecisaoProgressaoResponse Montar(
        PortalJanelaProgressaoResponse janela,
        PortalCicloEsportivoResponse? ciclo,
        PortalAcoesPrioritariasCicloResponse prioridades,
        PortalTendenciaObjetivoResponse tendenciaObjetivo)
    {
        if (!janela.JanelaAberta)
        {
            var estado = janela.Estado == "Revisar" ? "Revisar" : janela.Estado == "DadosInsuficientes" ? "DadosInsuficientes" : "Aguardar";
            return new(estado, "Progressao permanece em espera",
                "A decisao assistida respeita a janela anterior e nao contorna criterio pendente, recuperacao ou carga em revisao.",
                false, null, "ManterContexto", janela.Resumo,
                janela.Acao, Array.Empty<PortalOpcaoDecisaoProgressaoResponse>(),
                "Janela fechada bloqueia nova exigencia. Nao cria meta, nao aumenta carga e nao transforma falta de dado em falha.");
        }

        var opcoes = new List<PortalOpcaoDecisaoProgressaoResponse>();
        var prioridade = prioridades.Prioridades.OrderBy(x => x.Ordem).FirstOrDefault();
        var categoriaPrioridade = NormalizarCategoria(prioridade?.Categoria);

        if (categoriaPrioridade is not null)
            opcoes.Add(new("prioridade-ciclo", categoriaPrioridade, $"Discutir {Rotulo(categoriaPrioridade)}", "Primaria",
                $"Prioridade atual do ciclo: {prioridade!.Titulo}."));

        var categoriaObjetivo = CategoriaPorPerfil(ciclo?.PerfilEsportivo, ciclo?.Objetivo);
        if (categoriaObjetivo is not null && opcoes.All(x => x.Categoria != categoriaObjetivo))
            opcoes.Add(new("objetivo-ciclo", categoriaObjetivo, $"Alinhar {Rotulo(categoriaObjetivo)} ao objetivo", opcoes.Count == 0 ? "Primaria" : "Secundaria",
                $"Perfil/objetivo do ciclo: {ciclo?.PerfilEsportivo ?? tendenciaObjetivo.PerfilEsportivo}{(string.IsNullOrWhiteSpace(ciclo?.Objetivo) ? "" : $" • {ciclo!.Objetivo}")}."));

        if (opcoes.All(x => x.Categoria != "Consistencia"))
            opcoes.Add(new("consistencia-base", "Consistencia", "Preservar consistencia antes de ampliar exigencia", opcoes.Count == 0 ? "Primaria" : "Secundaria",
                "A base comportamental ja abriu a janela; consistencia continua sendo referencia para sustentar qualquer mudanca."));

        // No maximo 3 alternativas legiveis; nenhuma representa prescricao automatica.
        var selecionadas = opcoes.Take(3).ToList();
        var sugerida = selecionadas.First().Categoria;
        var evidencia = prioridade is not null
            ? $"A janela esta aberta e a primeira prioridade contextual do ciclo aponta para {Rotulo(sugerida)}."
            : $"A janela esta aberta; o perfil do ciclo aponta primeiro para {Rotulo(sugerida)}.";

        return new("Discutir", "Escolha assistida do proximo passo",
            "Ha espaco para discutir uma progressao, mas apenas um eixo deve ser considerado por vez.",
            true, sugerida, "DiscutirUmaProgressao", evidencia,
            $"Leve {Rotulo(sugerida)} para revisao com o profissional e mantenha os outros eixos estaveis ate observar a resposta.",
            selecionadas,
            "Categoria sugerida nao e prescricao. O sistema nao define carga, volume, calorias, medicacao nem cria meta automaticamente.");
    }

    private static string? NormalizarCategoria(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria)) return null;
        var c = categoria.Trim().ToLowerInvariant();
        if (c.Contains("recuper")) return "Recuperacao";
        if (c.Contains("nutri") || c.Contains("aliment")) return "Nutricao";
        if (c.Contains("treino") || c.Contains("performance") || c.Contains("carga")) return "Treino";
        if (c.Contains("consist") || c.Contains("ades") || c.Contains("rotina")) return "Consistencia";
        return "Consistencia";
    }

    private static string? CategoriaPorPerfil(string? perfil, string? objetivo)
    {
        var texto = $"{perfil} {objetivo}".ToLowerInvariant();
        if (texto.Contains("forca") || texto.Contains("força") || texto.Contains("hipertrof") || texto.Contains("corrida") || texto.Contains("resistencia") || texto.Contains("resistência")) return "Treino";
        if (texto.Contains("emagrec") || texto.Contains("composicao") || texto.Contains("composição") || texto.Contains("nutri")) return "Nutricao";
        if (texto.Contains("qualidade") || texto.Contains("saude") || texto.Contains("saúde")) return "Consistencia";
        return null;
    }

    private static string Rotulo(string categoria) => categoria switch
    {
        "Treino" => "treino/performance",
        "Nutricao" => "nutricao",
        "Recuperacao" => "recuperacao",
        _ => "consistencia"
    };
}
