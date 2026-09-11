using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class EvolucaoEsportivaService
{
    public static PortalEvolucaoEsportivaResponse Montar(
        PortalGamificacaoResponse gamificacao,
        PortalCicloEsportivoResponse? ciclo,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalCargaTreinoResponse carga,
        PortalPerformanceResponse performance,
        PortalAdesaoNutricionalResponse nutricao,
        PortalHidratacaoContextualResponse hidratacao)
    {
        var itens = new List<PortalEvolucaoEsportivaIndicadorResponse>();

        var consistenciaEstado = gamificacao.ConsistenciaScore >= 80 ? "Favoravel" : gamificacao.ConsistenciaScore >= 60 ? "Estavel" : "Atencao";
        itens.Add(new("consistencia", "Adesao", consistenciaEstado, "Consistência", $"{gamificacao.ConsistenciaScore}/100",
            "Frequência sustentável, não perfeição", gamificacao.StreakDias >= 3 ? "Mantendo" : "Construindo",
            $"{gamificacao.DiasAtivos14} dias ativos nos últimos 14 dias."));

        var recEstado = recuperacao.NivelAtencao == "Alta" ? "Atencao" : recuperacao.Tendencia == "Melhorando" ? "Favoravel" : recuperacao.DiasObservados < 3 ? "DadosInsuficientes" : "Estavel";
        itens.Add(new("recuperacao", "Recuperacao", recEstado, "Recuperação",
            recuperacao.ProntidaoMedia7.HasValue ? $"{recuperacao.ProntidaoMedia7:0}/100" : "—",
            recuperacao.SonoMedio7.HasValue ? $"Sono {recuperacao.SonoMedio7:0.0}h" : null,
            recuperacao.Tendencia, recuperacao.Mensagem));

        var cargaEstado = carga.NivelAtencao == "Alta" ? "Atencao" : carga.Classificacao == "DadosInsuficientes" ? "DadosInsuficientes" : "Estavel";
        itens.Add(new("carga", "Treino", cargaEstado, "Carga de treino",
            carga.RelacaoCargaComBase.HasValue ? $"{carga.RelacaoCargaComBase:0.00}x base" : carga.CargaInterna7.HasValue ? $"{carga.CargaInterna7:0} u.a." : "—",
            carga.RpeMedio7.HasValue ? $"RPE médio {carga.RpeMedio7:0.0}" : null,
            carga.Classificacao, carga.Mensagem));

        var perfEstado = performance.PrsRecentes > 0 || performance.Tendencia == "Evoluindo" ? "Favoravel" : performance.TreinosPeriodo == 0 ? "DadosInsuficientes" : "Estavel";
        itens.Add(new("performance", "Performance", perfEstado, "Performance",
            performance.PrsRecentes > 0 ? $"{performance.PrsRecentes} PR(s) recente(s)" : $"{performance.TreinosPeriodo} treinos",
            performance.VariacaoVolumePercentual.HasValue ? $"Volume {performance.VariacaoVolumePercentual:+0.0;-0.0;0}%" : null,
            performance.Tendencia, performance.Mensagem));

        var nutEstado = nutricao.RefeicoesRegistradas == 0 ? "DadosInsuficientes" : nutricao.AdequacaoRegistradaPercentual >= 75m ? "Favoravel" : nutricao.AdequacaoRegistradaPercentual < 50m ? "Atencao" : "Estavel";
        itens.Add(new("nutricao", "Nutricao", nutEstado, "Adesão nutricional",
            nutricao.AdequacaoRegistradaPercentual.HasValue ? $"{nutricao.AdequacaoRegistradaPercentual:0}% registrada" : "Sem registros",
            $"{nutricao.RefeicoesRegistradas}/{nutricao.RefeicoesPlanejadas} refeições registradas",
            nutricao.Estado, nutricao.Mensagem));

        var hidEstado = hidratacao.MetaMl is null ? "DadosInsuficientes" : hidratacao.NivelAtencao == "Alta" ? "Atencao" : hidratacao.ProgressoPercentual >= 75m ? "Favoravel" : "Estavel";
        itens.Add(new("hidratacao", "Autocuidado", hidEstado, "Hidratação",
            hidratacao.ProgressoPercentual.HasValue ? $"{hidratacao.ProgressoPercentual:0}% da meta" : "Sem meta",
            hidratacao.MetaMl.HasValue ? $"Meta {hidratacao.MetaMl:0} ml" : "Meta profissional não definida",
            hidratacao.Estado, hidratacao.Mensagem));

        if (ciclo is not null)
            itens.Add(new("ciclo", "Planejamento", "Estavel", "Ciclo esportivo", $"Semana {ciclo.SemanaAtual}/{ciclo.TotalSemanas}",
                $"{ciclo.ProgressoTemporalPercentual:0}% do período", ciclo.PerfilEsportivo, ciclo.Objetivo ?? ciclo.Nome));

        var validos = itens.Count(x => x.Estado != "DadosInsuficientes");
        var favoraveis = itens.Count(x => x.Estado == "Favoravel");
        var atencao = itens.Count(x => x.Estado == "Atencao");
        var estado = validos < 3 ? "DadosInsuficientes" : atencao >= 2 ? "Observar" : favoraveis >= 3 ? "Evoluindo" : "Estavel";
        var titulo = estado switch { "Evoluindo" => "Evolução consistente", "Observar" => "Evolução com pontos para revisar", "DadosInsuficientes" => "Construindo sua linha de evolução", _ => "Evolução estável" };
        var resumo = estado switch {
            "Evoluindo" => "Várias dimensões do ciclo mostram sinais favoráveis sem depender de um único indicador.",
            "Observar" => "Há mais de uma dimensão pedindo atenção. Use o detalhe de cada indicador para decidir o próximo passo.",
            "DadosInsuficientes" => "Continue registrando o dia a dia para formar uma leitura longitudinal confiável.",
            _ => "As principais dimensões estão relativamente estáveis no período atual."
        };
        return new(estado, titulo, resumo, favoraveis, atencao, itens,
            "Este painel não é um score clínico, não diagnostica condições e não substitui a interpretação do profissional ou a estratégia do ciclo.");
    }
}
