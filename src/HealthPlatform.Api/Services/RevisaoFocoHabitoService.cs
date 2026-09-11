using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Revisa o foco atual sem criar uma nova meta automaticamente. A decisao e manter, consolidar, proteger ou permanecer em manutencao.
/// </summary>
public static class RevisaoFocoHabitoService
{
    public static PortalRevisaoFocoHabitoResponse Montar(
        PortalProximoFocoHabitoResponse proximoFoco,
        PortalEstabilidadeHabitosResponse estabilidade,
        PortalProtecaoRetomadaResponse protecaoRetomada,
        PortalTendenciaSemanalResponse tendenciaSemanal)
    {
        var manutencao = proximoFoco.HabitosEmManutencao.Count;

        if (protecaoRetomada.Estado == "Proteger" || protecaoRetomada.RetomadaEmCurso)
            return new("Proteger", "Pause nova cobranca",
                "A retomada ainda merece protecao. O foco atual nao deve virar uma exigencia maior nesta semana.",
                proximoFoco.CodigoFoco, proximoFoco.CategoriaFoco, proximoFoco.EstadoDoFoco, "Proteger",
                "Ha sinais de retomada ou continuidade ainda fragil.",
                "Repita apenas os comportamentos que ja voltaram a acontecer e preserve recuperacao/descanso quando indicado.",
                manutencao,
                "A revisao do foco nao cria meta nova, nao remove XP, nao pune streak e nao altera treino, nutricao ou medicacao automaticamente.");

        if (string.IsNullOrWhiteSpace(proximoFoco.CodigoFoco))
            return new("Manutencao", "Mantenha o que ja funciona",
                "Nao existe um foco unico que precise de nova cobranca agora.", null, null, null, "Manter",
                $"{manutencao} habito(s) ja estao em manutencao.",
                "Continue a rotina atual e espere um sinal observavel antes de escolher outro foco.", manutencao,
                "Estabilidade nao gera meta extra. O sistema evita criar desafio apenas para manter engajamento.");

        var atual = estabilidade.Itens.FirstOrDefault(x => x.Codigo == proximoFoco.CodigoFoco);
        var tendencia = TendenciaDoFoco(proximoFoco.CategoriaFoco, tendenciaSemanal);
        var evidenciaTendencia = tendencia is null ? "Sem comparacao semanal suficiente para este eixo."
            : $"Comparativo semanal: {tendencia.Estado} ({tendencia.ValorAnterior} -> {tendencia.ValorAtual}).";

        if (atual?.Estado == "Oscilando")
            return new("Continuar", "Mantenha um unico foco",
                "O comportamento ainda oscila; trocar de foco agora adicionaria complexidade sem consolidar a rotina.",
                atual.Codigo, atual.Categoria, atual.Estado, "Continuar",
                $"{atual.Evidencia} {evidenciaTendencia}",
                atual.Orientacao, manutencao,
                "Continuar um foco nao significa aumentar exigencia. A acao permanece pequena, repetivel e subordinada a recuperacao e ao plano profissional.");

        if (atual?.Estado == "Consolidando")
            return new("Consolidar", "Consolide antes de encerrar",
                "O foco mostra sinais de consolidacao. Repita o comportamento antes de abrir uma nova frente.",
                atual.Codigo, atual.Categoria, atual.Estado, "Consolidar",
                $"{atual.Evidencia} {evidenciaTendencia}",
                "Repita o mesmo comportamento nesta janela; nao aumente a meta so porque houve melhora.", manutencao,
                "Consolidacao prioriza repeticao sustentavel, nao perfeicao. Nenhuma prescricao e alterada automaticamente.");

        return new("Manutencao", "Foco pronto para sair de evidência",
            "O eixo nao apresenta oscilacao relevante agora. Mantenha-o sem adicionar nova cobranca imediatamente.",
            proximoFoco.CodigoFoco, proximoFoco.CategoriaFoco, atual?.Estado ?? proximoFoco.EstadoDoFoco, "Manter",
            evidenciaTendencia, "Mantenha o comportamento e deixe um novo foco surgir apenas quando os dados justificarem.", manutencao,
            "Encerrar destaque nao significa abandonar o habito; significa coloca-lo em manutencao sem pressao adicional.");
    }

    private static PortalTendenciaSemanalItemResponse? TendenciaDoFoco(string? categoria, PortalTendenciaSemanalResponse tendencia)
    {
        var codigo = categoria switch
        {
            "Rotina" => "dias-ativos",
            "Nutricao" => "nutricao",
            "Hidratacao" => "hidratacao",
            "Recuperacao" => "prontidao",
            _ => null
        };
        return codigo is null ? null : tendencia.Itens.FirstOrDefault(x => x.Codigo == codigo);
    }
}
