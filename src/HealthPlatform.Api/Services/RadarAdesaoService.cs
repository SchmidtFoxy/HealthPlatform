using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Resume sinais de afastamento do plano sem rotular o paciente e sem confundir descanso/recuperação prescritos com baixa adesão.
/// Não produz diagnóstico, probabilidade de abandono nem score oculto.
/// </summary>
public static class RadarAdesaoService
{
    public static PortalRadarAdesaoResponse Montar(
        PortalGamificacaoResponse gamificacao,
        PortalTendenciaSemanalResponse tendenciaSemanal,
        PortalPlanejamentoSemanalResponse planejamento,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalAdesaoNutricionalResponse nutricao,
        PortalHidratacaoContextualResponse hidratacao)
    {
        var itens = new List<PortalRadarAdesaoItemResponse>();
        var contextoRecuperacao = planejamento.Estado == "Proteger"
            || carga.Classificacao == "Revisar"
            || carga.NivelAtencao is "Alta" or "Atencao"
            || recuperacao.NivelAtencao is "Alta" or "Atencao";

        if (gamificacao.ConsistenciaScore < 50)
            itens.Add(new("consistencia", "Consistência", "Atencao", "Rotina perdeu continuidade",
                $"Consistência atual: {gamificacao.ConsistenciaScore}/100 • {gamificacao.DiasAtivos14} dia(s) ativo(s) em 14.",
                "Retome uma ação simples e sustentável do plano; não tente compensar tudo no mesmo dia."));
        else if (gamificacao.ConsistenciaScore < 70)
            itens.Add(new("consistencia", "Consistência", "Observar", "Consistência oscilando",
                $"Consistência atual: {gamificacao.ConsistenciaScore}/100.",
                "Use as prioridades da semana para escolher a próxima ação possível."));

        var diasAtivos = tendenciaSemanal.Itens.FirstOrDefault(x => x.Codigo == "dias-ativos");
        if (!contextoRecuperacao && diasAtivos?.Estado == "Menor")
            itens.Add(new("ritmo", "Execução", "Observar", "Menos dias ativos que na semana anterior",
                $"Dias ativos: {diasAtivos.ValorAtual} vs {diasAtivos.ValorAnterior}.",
                "Confira se houve barreira de rotina e retome pelo próximo item planejado, sem compensação."));
        else if (contextoRecuperacao && diasAtivos?.Estado == "Menor")
            itens.Add(new("ritmo-recuperacao", "Execução", "Contexto", "Ritmo menor com contexto de recuperação",
                $"Dias ativos: {diasAtivos.ValorAtual} vs {diasAtivos.ValorAnterior}.",
                "Não trate descanso ou redução de carga coerente com recuperação como falha de adesão."));

        if (nutricao.AdequacaoRegistradaPercentual.HasValue && nutricao.AdequacaoRegistradaPercentual.Value < 60m)
            itens.Add(new("nutricao", "Nutrição", "Observar", "Execução alimentar caiu",
                $"Adequação entre refeições registradas: {nutricao.AdequacaoRegistradaPercentual:0}%.",
                "Retome as refeições previstas sem jejum, restrição ou excesso para compensar."));

        if (hidratacao.MetaMl.HasValue && hidratacao.ProgressoPercentual.HasValue && hidratacao.ProgressoPercentual.Value < 60m)
            itens.Add(new("hidratacao", "Hidratação", "Observar", "Meta hídrica está atrasada",
                $"{hidratacao.ProgressoPercentual:0}% da meta hídrica definida foi registrada hoje.",
                "Distribua o restante da meta já definida ao longo do dia; não aumente a meta automaticamente."));

        var sinaisAtencao = itens.Count(x => x.Estado == "Atencao" || x.Estado == "Observar");
        var dadosSuficientes = gamificacao.DiasAtivos14 > 0 || tendenciaSemanal.Itens.Any(x => x.Estado != "SemDados");
        var estado = !dadosSuficientes ? "DadosInsuficientes"
            : sinaisAtencao >= 3 ? "Reconectar"
            : sinaisAtencao >= 1 ? "Observar"
            : "Estavel";

        var titulo = estado switch
        {
            "Reconectar" => "Reconecte com o plano",
            "Observar" => "Adesão pede atenção leve",
            "DadosInsuficientes" => "Radar de adesão em formação",
            _ => "Adesão estável"
        };
        var resumo = estado switch
        {
            "Reconectar" => "Há vários sinais de perda de continuidade. O foco é retomar ações pequenas e sustentáveis, não compensar o que ficou para trás.",
            "Observar" => "Há um ou poucos sinais de oscilação; use-os como contexto para facilitar a próxima ação.",
            "DadosInsuficientes" => "Ainda faltam registros suficientes para reconhecer um padrão de adesão.",
            _ => "Sem sinal relevante de afastamento do plano nos dados disponíveis."
        };

        return new(estado, titulo, resumo, sinaisAtencao, contextoRecuperacao, itens.Take(4).ToList(),
            "O radar identifica sinais de continuidade nos registros; não rotula o paciente, não calcula probabilidade de abandono, não pune XP e não altera treino, nutrição ou medicação automaticamente. Descanso coerente com recuperação não é tratado como falha de adesão.");
    }
}
