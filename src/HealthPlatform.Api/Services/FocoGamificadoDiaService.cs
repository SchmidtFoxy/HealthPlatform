using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class FocoGamificadoDiaService
{
    public static PortalFocoGamificadoDiaResponse Montar(
        PortalGamificacao2Response gamificacao2,
        PortalMissoesContextuais2Response missoes)
    {
        // O foco do dia reduz a gamificacao a uma prioridade legivel no celular.
        // Ele reaproveita contexto e progresso existentes; nao cria treino, meta, XP ou prescricao paralela.
        var itens = missoes.Missoes ?? Array.Empty<PortalMissaoContextual2ItemResponse>();
        var pendentes = itens.Where(x => !x.Concluido).ToArray();
        var protegidoHoje = pendentes.Any(x => x.EstadoContextual == "SemPressaoHoje");

        if (pendentes.Length == 0)
        {
            return new(
                "SemanaEmDia", "Foco gamificado do dia", "Manutencao",
                "Missões da semana concluídas",
                "Mantenha o que já funciona sem adicionar volume ou intensidade apenas para buscar mais recompensa.",
                null, null, 0, 0, 0, false,
                "Nada extra precisa ser feito para proteger XP, nível ou streak.",
                Seguranca);
        }

        if (protegidoHoje)
        {
            return new(
                "DiaProtegido", "Foco gamificado do dia", "Recuperacao",
                gamificacao2.FocoAtual,
                "Hoje a prioridade é respeitar o contexto esportivo; as missões permanecem visíveis, mas sem pressão para avançar.",
                null, null, 0, 0, 0, true,
                "Recuperação planejada, revisão ou retorno gradual têm precedência sobre completar missão, XP ou streak.",
                Seguranca);
        }

        // Uma unica missao vira referencia do dia para reduzir ruido de decisao; continua sendo referencia, nao obrigacao.
        var alvo = pendentes.First();
        var restante = Math.Max(0, alvo.Meta - alvo.Progresso);
        return new(
            "ReferenciaDoDia", "Foco gamificado do dia", "Consistencia",
            alvo.Titulo,
            alvo.OrientacaoContextual,
            alvo.Codigo, alvo.Titulo, alvo.Progresso, alvo.Meta, alvo.RecompensaXp, false,
            restante == 0
                ? "O progresso necessario ja foi registrado; aguarde a consolidacao normal da missao."
                : $"Faltam {restante} unidade(s) na meta semanal; avance somente se isso ja estiver coerente com o plano de hoje.",
            Seguranca);
    }

    private const string Seguranca =
        "Foco Gamificado do Dia nao cria treino, nao altera meta semanal, nao concede XP por si so, nao exige compensacao e nao autoriza aumento automatico de carga, volume ou intensidade.";
}
