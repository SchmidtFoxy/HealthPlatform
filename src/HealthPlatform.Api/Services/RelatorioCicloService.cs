using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class RelatorioCicloService
{
    public static PortalRelatorioCicloResponse Montar(
        PortalCicloEsportivoResponse? ciclo,
        PortalMetasCicloResponse metas,
        PortalCheckpointCicloResponse checkpoint,
        PortalEvolucaoEsportivaResponse evolucao)
    {
        if (ciclo is null)
            return new("SemCiclo", "Relatório do ciclo", "Nenhum ciclo esportivo ativo para consolidar.", "Sem período ativo", 0, 0, 0, 0, null,
                Array.Empty<PortalRelatorioCicloItemResponse>(),
                "O relatório consolida dados registrados; não cria diagnóstico, prescrição ou próximo ciclo automaticamente.");

        var itens = new List<PortalRelatorioCicloItemResponse>();
        var periodo = $"{ciclo.DataInicio:dd/MM/yyyy} a {ciclo.DataFim:dd/MM/yyyy}";

        itens.Add(new("volume-ciclo", "Execução", "Informativo", "Volume realizado no ciclo",
            $"{ciclo.TreinosNoCiclo} treino(s) concluído(s) • {ciclo.CheckInsNoCiclo} check-in(s) • prontidão média {(ciclo.MediaProntidao.HasValue ? $"{ciclo.MediaProntidao:0.0}/100" : "sem dados")}.",
            "Use o volume realizado como contexto de adesão; quantidade isolada não define qualidade do ciclo."));

        var metasNumericas = metas.Itens.Where(x => x.ProgressoPercentual.HasValue).ToList();
        var metasAtingidas = metasNumericas.Count(x => x.Estado == "Atingida");
        var metasObservar = metas.Itens.Count(x => x.Estado == "Observar");
        itens.Add(new("metas-ciclo", "Metas", metasObservar > 0 ? "Revisar" : metasAtingidas == metasNumericas.Count && metasNumericas.Count > 0 ? "Atingidas" : "EmCurso",
            "Metas mensuráveis",
            metasNumericas.Count == 0 ? "Nenhuma meta mensurável configurada." : $"{metasAtingidas}/{metasNumericas.Count} meta(s) mensurável(is) atingida(s); {metasObservar} pedindo observação.",
            "Leia cada meta separadamente; o relatório não converte objetivos diferentes em um score único."));

        itens.Add(new("evolucao-multidimensional", "Evolução", evolucao.IndicadoresAtencao > 0 ? "Revisar" : evolucao.Estado,
            "Evolução multidimensional",
            $"{evolucao.IndicadoresFavoraveis} dimensão(ões) favorável(is) • {evolucao.IndicadoresAtencao} em atenção.",
            "Consistência, recuperação, carga, performance, nutrição e hidratação continuam visíveis como dimensões independentes."));

        itens.Add(new("checkpoint-final", "Revisão", checkpoint.Estado, "Checkpoint do ciclo",
            $"Semana {checkpoint.SemanaAtual}/{checkpoint.TotalSemanas} • {checkpoint.ProgressoTemporalPercentual:0}% do período • estado {checkpoint.Estado}.",
            checkpoint.Estado == "Revisar"
                ? "Revise as evidências sinalizadas antes de encerrar ou planejar o ciclo seguinte."
                : checkpoint.Estado == "Consolidar"
                    ? "Consolide resultados e faça a revisão profissional antes de iniciar outro ciclo."
                    : "Mantenha o acompanhamento até o ciclo estar pronto para fechamento."));

        var revisar = checkpoint.Estado == "Revisar" || metasObservar > 0 || evolucao.IndicadoresAtencao >= 2;
        var prontoFechamento = !revisar && ciclo.ProgressoTemporalPercentual >= 85m;
        var evoluindo = !revisar && !prontoFechamento && evolucao.Estado == "Evoluindo";
        var estado = revisar ? "Revisar" : prontoFechamento ? "ProntoParaFechamento" : evoluindo ? "Evoluindo" : "EmCurso";
        var resumo = estado switch
        {
            "Revisar" => "O ciclo possui evidências que merecem revisão profissional antes do fechamento ou da próxima estratégia.",
            "ProntoParaFechamento" => "O ciclo está perto do fim e sem sinais críticos no resumo atual; consolide os resultados com o profissional.",
            "Evoluindo" => "O ciclo segue com sinais favoráveis; continue acompanhando as dimensões até o fechamento.",
            _ => "O ciclo ainda está em andamento; o relatório funciona como fotografia longitudinal do progresso registrado."
        };

        return new(estado, "Relatório de evolução do ciclo", resumo, periodo, ciclo.SemanaAtual, ciclo.TotalSemanas,
            ciclo.TreinosNoCiclo, ciclo.CheckInsNoCiclo, ciclo.MediaProntidao, itens,
            "Relatório não é diagnóstico, não é score clínico, não altera prescrição e não cria o próximo ciclo automaticamente.");
    }
}
