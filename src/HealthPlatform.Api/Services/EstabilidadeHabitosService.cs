using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Mostra quais comportamentos ja estao estaveis, quais ainda consolidam e quais oscilam.
/// Nao cria score oculto, nao pune o usuario e nao transforma descanso coerente em falha de habito.
/// </summary>
public static class EstabilidadeHabitosService
{
    public static PortalEstabilidadeHabitosResponse Montar(
        PortalGamificacaoResponse gamificacao,
        PortalTendenciaSemanalResponse tendenciaSemanal,
        PortalRadarAdesaoResponse radar,
        PortalProtecaoRetomadaResponse protecaoRetomada)
    {
        var itens = new List<PortalEstabilidadeHabitoItemResponse>();

        var consistenciaEstado = gamificacao.ConsistenciaScore >= 80 ? "Estavel"
            : gamificacao.ConsistenciaScore >= 60 ? "Consolidando" : "Oscilando";
        itens.Add(new("consistencia", "Rotina", consistenciaEstado, "Consistencia da rotina",
            $"Consistencia atual: {gamificacao.ConsistenciaScore}/100 em {gamificacao.DiasAtivos14}/14 dias ativos.",
            consistenciaEstado == "Estavel" ? "Mantenha o ritmo sem adicionar cobranca so porque o habito estabilizou."
            : "Priorize repeticao simples e sustentavel antes de ampliar metas."));

        AdicionarTendencia(itens, tendenciaSemanal, "dias-ativos", "Rotina", "Dias ativos");
        AdicionarTendencia(itens, tendenciaSemanal, "nutricao", "Nutricao", "Adesao alimentar");
        AdicionarTendencia(itens, tendenciaSemanal, "hidratacao", "Hidratacao", "Hidratacao");

        if (radar.ContextoRecuperacao)
            itens.Add(new("recuperacao", "Recuperacao", "Contexto", "Descanso tambem consolida rotina",
                "Ha contexto de recuperacao/carga justificando menor ritmo.",
                "Nao transforme descanso coerente em oscilacao de habito; preserve a estrategia do dia."));

        itens = itens.Where(x => x.Estado != "SemDados" || x.Codigo is "nutricao" or "hidratacao").Take(5).ToList();
        var estaveis = itens.Count(x => x.Estado == "Estavel");
        var consolidando = itens.Count(x => x.Estado == "Consolidando");
        var oscilando = itens.Count(x => x.Estado == "Oscilando");

        var estado = tendenciaSemanal.Estado == "DadosInsuficientes" && gamificacao.DiasAtivos14 < 3 ? "DadosInsuficientes"
            : protecaoRetomada.Estado == "Proteger" || oscilando >= 2 ? "Oscilando"
            : consolidando > 0 || protecaoRetomada.RetomadaEmCurso ? "Consolidando"
            : "Estavel";
        var titulo = estado switch
        {
            "Oscilando" => "Alguns habitos ainda oscilam",
            "Consolidando" => "Rotina em consolidacao",
            "DadosInsuficientes" => "Estabilidade em formacao",
            _ => "Habitos ganhando estabilidade"
        };
        var resumo = estado switch
        {
            "Oscilando" => "Concentre suporte nos eixos que variaram, sem aumentar cobranca nos que ja estao estaveis.",
            "Consolidando" => "Parte da rotina ja mostra repeticao; mantenha poucos comportamentos ate eles ficarem mais automaticos.",
            "DadosInsuficientes" => "Ainda faltam registros comparaveis para diferenciar habito estavel de variacao ocasional.",
            _ => "Os principais comportamentos acompanhados estao consistentes nos dados disponiveis."
        };

        return new(estado, titulo, resumo, estaveis, consolidando, oscilando, itens,
            "Estabilidade de habito e uma leitura de repeticao observada, nao diagnostico nem garantia de permanencia. O sistema nao remove XP, nao pune streak e nao altera treino, nutricao ou medicacao automaticamente.");
    }

    private static void AdicionarTendencia(List<PortalEstabilidadeHabitoItemResponse> itens, PortalTendenciaSemanalResponse tendencia, string codigo, string categoria, string titulo)
    {
        var eixo = tendencia.Itens.FirstOrDefault(x => x.Codigo == codigo);
        if (eixo is null) return;
        var estado = eixo.Estado switch
        {
            "Estavel" => "Estavel",
            "Subiu" or "Maior" => "Consolidando",
            "Caiu" or "Menor" => "Oscilando",
            _ => "SemDados"
        };
        itens.Add(new(codigo, categoria, estado, titulo, $"Atual: {eixo.ValorAtual} • anterior: {eixo.ValorAnterior}.",
            estado == "Estavel" ? "Mantenha o comportamento sem adicionar meta extra."
            : estado == "Consolidando" ? "Repita o que funcionou antes de elevar exigencia."
            : estado == "Oscilando" ? "Escolha uma proxima acao simples; nao compense tudo de uma vez."
            : "Continue registrando para formar uma linha comparavel."));
    }
}
