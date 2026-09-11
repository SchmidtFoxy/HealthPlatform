using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Avalia se existe espaco para um novo desafio depois da consolidacao de um habito.
/// Nao cria meta, nao aumenta meta e nao substitui revisao profissional/planejamento do ciclo.
/// </summary>
public static class ReentradaDesafioService
{
    public static PortalReentradaDesafioResponse Montar(
        PortalEncerramentoCicloHabitoResponse encerramento,
        PortalEstabilidadeHabitosResponse estabilidade,
        PortalProtecaoRetomadaResponse protecaoRetomada)
    {
        var categoriaBase = encerramento.CategoriaHabito;

        if (protecaoRetomada.Estado == "Proteger" || encerramento.Estado == "Proteger")
            return new("Proteger", "Ainda nao e hora de aumentar exigencia",
                "A retomada ainda precisa de estabilidade antes de qualquer novo desafio.", false, categoriaBase,
                "Preservar", "Existe contexto de protecao da retomada.",
                "Mantenha apenas o que ja voltou a funcionar e reavalie depois; nao aumente a meta.",
                estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
                "Protecao vem antes de progressao. O sistema nao cria nova meta automatica e nao pune XP ou streak.");

        if (!encerramento.EmManutencao)
            return new("Aguardar", "Consolide o foco atual primeiro",
                "O habito ainda nao saiu da janela de atencao ativa.", false, categoriaBase,
                "Continuar", encerramento.Evidencia,
                "Repita a mesma acao pequena ate o foco entrar em manutencao; nao abra outra frente agora.",
                estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
                "Novo desafio so e considerado depois da consolidacao; nao ha troca automatica de foco.");

        if (estabilidade.HabitosOscilando > 0)
            return new("Aguardar", "Existe outro habito oscilando",
                "A base ainda tem um comportamento pedindo suporte; adicionar exigencia agora aumentaria complexidade.", false, categoriaBase,
                "EstabilizarBase", $"{estabilidade.HabitosOscilando} habito(s) ainda oscilando.",
                "Priorize estabilidade antes de iniciar qualquer novo desafio.",
                estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
                "Oscilacao tem prioridade sobre progressao. O sistema nao gera nova meta automatica.");

        if (estabilidade.HabitosConsolidando > 0)
            return new("Consolidar", "Deixe a base amadurecer",
                "Nao ha oscilacao relevante, mas ainda existem comportamentos em consolidacao.", false, categoriaBase,
                "ConsolidarBase", $"{estabilidade.HabitosConsolidando} habito(s) ainda consolidando.",
                "Mantenha a rotina por mais uma janela antes de adicionar nova exigencia.",
                estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
                "Melhora nao exige progressao imediata; nao aumente a meta so porque a rotina melhorou.");

        if (estabilidade.HabitosEstaveis >= 2)
            return new("EspacoParaDesafio", "Existe espaco para um novo desafio",
                "A rotina esta em manutencao, sem oscilacao relevante e com base estavel suficiente para considerar uma unica progressao.", true, categoriaBase,
                "ConsiderarUmDesafio", $"{estabilidade.HabitosEstaveis} habitos estaveis e nenhum eixo oscilando/consolidando.",
                "Se fizer sentido para o ciclo e para o profissional, escolha apenas um desafio pequeno; o sistema nao cria a meta automaticamente.",
                estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
                "Elegibilidade nao e prescricao. Nao aumenta carga, nutricao, medicacao ou meta automaticamente.");

        return new("Manutencao", "Mantenha antes de progredir",
            "A rotina esta tranquila, mas ainda ha pouca base estavel para justificar uma nova exigencia.", false, categoriaBase,
            "Manter", "Menos de dois habitos aparecem como estaveis nesta janela.",
            "Continue mantendo o que funciona; nao e necessario criar um novo desafio agora.",
            estabilidade.HabitosEstaveis, estabilidade.HabitosConsolidando, estabilidade.HabitosOscilando,
            "Manutencao e um estado valido. O sistema nao cria desafios apenas para manter engajamento.");
    }
}
