using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Mantem habitos estaveis em manutencao e escolhe, quando apropriado, apenas um foco de melhoria por vez.
/// Nao cria nova prescricao, nao aumenta meta automaticamente e respeita contexto de recuperacao/retomada.
/// </summary>
public static class ProximoFocoHabitoService
{
    public static PortalProximoFocoHabitoResponse Montar(
        PortalEstabilidadeHabitosResponse estabilidade,
        PortalProtecaoRetomadaResponse protecaoRetomada,
        PortalRadarAdesaoResponse radar)
    {
        var manutencao = estabilidade.Itens
            .Where(x => x.Estado == "Estavel")
            .Select(x => x.Titulo)
            .Distinct()
            .Take(4)
            .ToList();

        if (protecaoRetomada.Estado == "Proteger" || protecaoRetomada.RetomadaEmCurso)
        {
            return new(
                "Proteger",
                "Primeiro consolide a retomada",
                "Este nao e o momento de adicionar um novo foco. Preserve os poucos comportamentos que ja voltaram a acontecer.",
                null, null, null,
                "Ha sinais de retomada ainda em consolidacao.",
                "Repita a proxima acao simples do plano de reconexao e mantenha o restante em manutencao.",
                manutencao,
                "Protecao da retomada vem antes de novas metas. O sistema nao remove XP, nao pune streak e nao altera treino, nutricao ou medicacao automaticamente.");
        }

        var foco = estabilidade.Itens
            .Where(x => x.Estado == "Oscilando")
            .OrderBy(x => PrioridadeCategoria(x.Categoria))
            .FirstOrDefault()
            ?? estabilidade.Itens
                .Where(x => x.Estado == "Consolidando")
                .OrderBy(x => PrioridadeCategoria(x.Categoria))
                .FirstOrDefault();

        if (foco is null)
        {
            var resumo = radar.Estado == "Estavel"
                ? "Os principais habitos acompanhados estao estaveis. Mantenha o que funciona sem aumentar a cobranca."
                : "Nao ha um unico eixo claro para priorizar agora; mantenha a rotina e continue registrando.";
            return new(
                "Manter",
                "Rotina em modo de manutencao",
                resumo,
                null, null, null, null,
                "Nao adicione uma meta nova apenas porque os habitos atuais estabilizaram.",
                manutencao,
                "Habito estavel nao gera cobranca extra. O proximo foco so aparece quando existe um eixo observavel para consolidar.");
        }

        var estado = foco.Estado == "Oscilando" ? "FocoPrioritario" : "FocoDeConsolidacao";
        var titulo = foco.Estado == "Oscilando" ? "Um foco por vez" : "Consolide antes de ampliar";
        var resumoFoco = foco.Estado == "Oscilando"
            ? $"{foco.Titulo} e o eixo mais util para receber atencao agora; os habitos estaveis ficam apenas em manutencao."
            : $"{foco.Titulo} ainda esta consolidando. Repita o comportamento antes de aumentar exigencia.";

        return new(
            estado, titulo, resumoFoco, foco.Codigo, foco.Categoria, foco.Estado, foco.Evidencia, foco.Orientacao, manutencao,
            "O proximo foco organiza atencao, nao cria nova prescricao. Nao aumenta carga, calorias, restricao, volume ou meta automaticamente.");
    }

    private static int PrioridadeCategoria(string categoria) => categoria switch
    {
        "Recuperacao" => 0,
        "Rotina" => 1,
        "Nutricao" => 2,
        "Hidratacao" => 3,
        _ => 4
    };
}
