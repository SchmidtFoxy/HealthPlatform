using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Fecha a janela de atencao de um habito quando os sinais deixam de justificar foco ativo.
/// Nao cria um novo foco automaticamente, nao persiste rotulos de recaida e nao aumenta cobranca.
/// </summary>
public static class EncerramentoCicloHabitoService
{
    public static PortalEncerramentoCicloHabitoResponse Montar(
        PortalRevisaoFocoHabitoResponse revisao,
        PortalEstabilidadeHabitosResponse estabilidade,
        PortalProtecaoRetomadaResponse protecaoRetomada)
    {
        var manutencao = Math.Max(revisao.HabitosEmManutencao, estabilidade.HabitosEstaveis);

        if (protecaoRetomada.Estado == "Proteger" || revisao.Decisao == "Proteger")
            return new("Proteger", "Proteja a retomada antes de encerrar foco",
                "A rotina ainda precisa de uma janela de protecao; encerrar ou trocar o foco agora pode adicionar cobranca desnecessaria.",
                revisao.CodigoFoco, revisao.CategoriaFoco, "Proteger", revisao.Evidencia,
                "Mantenha apenas os comportamentos que ja voltaram a acontecer e reavalie quando a retomada estiver mais estavel.",
                false, manutencao,
                "Protecao vem antes de encerramento. O sistema nao remove XP, nao pune streak e nao altera treino, nutricao ou medicacao automaticamente.");

        if (revisao.Decisao == "Continuar")
            return new("EmCurso", "Foco ainda em curso",
                "O comportamento ainda oscila e nao esta pronto para sair da janela de atencao.",
                revisao.CodigoFoco, revisao.CategoriaFoco, "Continuar", revisao.Evidencia,
                "Repita a mesma acao pequena; nao abra um novo foco enquanto este eixo ainda oscila.",
                false, manutencao,
                "Continuar nao significa aumentar exigencia. O foco permanece pequeno, repetivel e subordinado a recuperacao e ao plano profissional.");

        if (revisao.Decisao == "Consolidar")
            return new("Consolidando", "Consolide antes de colocar em manutencao",
                "O comportamento melhorou, mas ainda merece mais uma janela de repeticao antes de sair do destaque.",
                revisao.CodigoFoco, revisao.CategoriaFoco, "Consolidar", revisao.Evidencia,
                "Repita o comportamento e nao aumente a meta; a manutencao vem depois da repeticao sustentavel.",
                false, manutencao,
                "Consolidacao nao e perfeicao. O sistema nao gera nova meta automatica so porque houve melhora.");

        if (!string.IsNullOrWhiteSpace(revisao.CodigoFoco))
            return new("ProntoParaManutencao", "Foco pronto para sair do destaque",
                "O eixo nao apresenta oscilacao relevante agora e pode voltar ao modo de manutencao.",
                revisao.CodigoFoco, revisao.CategoriaFoco, "EncerrarFoco", revisao.Evidencia,
                "Mantenha o comportamento sem adicionar um novo desafio nesta mesma janela.",
                true, manutencao + 1,
                "Encerrar destaque nao significa abandonar o habito. Significa reduzir cobranca e observar estabilidade antes de escolher outro foco.");

        return new("Manutencao", "Rotina em manutencao",
            "Nao ha foco ativo para encerrar. Os comportamentos estaveis seguem acompanhados sem nova cobranca.",
            null, null, "Manter", revisao.Evidencia,
            "Continue o que funciona e deixe um novo foco surgir apenas quando os dados justificarem.",
            true, manutencao,
            "Manutencao nao dispara automaticamente outra meta. O sistema evita criar desafio apenas para manter engajamento.");
    }
}
