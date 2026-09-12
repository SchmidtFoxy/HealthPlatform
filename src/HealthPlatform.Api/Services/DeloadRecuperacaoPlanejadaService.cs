using HealthPlatform.Api.Contracts.Portal;

namespace HealthPlatform.Api.Services;

public static class DeloadRecuperacaoPlanejadaService
{
    public static PortalDeloadRecuperacaoPlanejadaResponse Montar(
        PortalBlocoTreinamentoResponse bloco,
        PortalCargaIndividualizadaResponse cargaIndividualizada,
        PortalRespostaSessaoResponse respostaSessao,
        PortalRadarAdesaoResponse radarAdesao)
    {
        const string seguranca = "Deload/recuperacao planejada e intencao explicita do profissional: reducao deliberada de carga nao e baixa adesao, nao e falha e nao autoriza compensacao posterior. O sistema nao cria deload automaticamente, nao prescreve carga e nao diagnostica fadiga ou lesao.";

        if (bloco.Estado is "SemCiclo" or "SemBlocoConfigurado" || string.IsNullOrWhiteSpace(bloco.Nome))
            return new("SemPlanejamento", false, "Nenhum", "Sem recuperacao planejada identificada",
                "Nao ha bloco configurado suficiente para identificar uma intencao explicita de deload/recuperacao.", bloco.Nome, bloco.SemanaAtual,
                cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado, radarAdesao.Estado, Array.Empty<string>(), seguranca);

        // intencao explicita do profissional: somente termos registrados no bloco/fase podem caracterizar deload ou recuperacao planejada.
        var texto = $"{bloco.Nome} {bloco.Tipo} {bloco.Objetivo}".ToLowerInvariant();
        var termosDeload = new[] { "deload", "descarga", "regener", "recuper", "taper", "redução", "reducao" };
        var intencaoPlanejada = termosDeload.Any(texto.Contains);

        if (!intencaoPlanejada)
            return new("NaoPlanejado", false, "Nenhum", "Bloco sem deload planejado",
                "O bloco atual nao registra intencao explicita de reducao planejada. Quedas de execucao continuam sendo interpretadas pelo contexto real, sem inferir deload.",
                bloco.Nome, bloco.SemanaAtual, cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado, radarAdesao.Estado,
                new[] { "Nenhum marcador explicito de deload/recuperacao foi encontrado no bloco configurado." }, seguranca);

        var tipo = texto.Contains("taper") ? "Taper" : texto.Contains("deload") || texto.Contains("descarga") ? "Deload" : "RecuperacaoPlanejada";
        var sinais = new List<string>
        {
            $"Intencao profissional registrada no bloco: {bloco.Nome} ({bloco.Tipo}).",
            $"Semana atual do bloco: {bloco.SemanaAtual?.ToString() ?? "—"}.",
            $"Carga atual versus historico individual: {cargaIndividualizada.PosicaoHistorica}.",
            $"Resposta recente a sessao: {respostaSessao.Estado}."
        };
        if (radarAdesao.Estado is "Observar" or "Reconectar")
            sinais.Add("O radar de adesao deve ser lido com contexto: esta reducao foi planejada e nao deve ser tratada isoladamente como queda de continuidade.");

        var estado = bloco.Estado == "Planejado" ? "Planejado" : bloco.Estado == "EmCurso" ? "EmCurso" : "Historico";
        var resumo = estado == "EmCurso"
            ? "Reducao planejada em curso. Preserve a intencao do bloco e avalie recuperacao/resposta antes de qualquer nova progressao."
            : estado == "Planejado"
                ? "Existe uma janela de deload/recuperacao configurada pelo profissional e ainda nao iniciada."
                : "A intencao de recuperacao faz parte do historico do bloco e deve permanecer distinguida de baixa adesao.";

        return new(estado, true, tipo, "Deload & recuperacao planejada", resumo, bloco.Nome, bloco.SemanaAtual,
            cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado, radarAdesao.Estado, sinais, seguranca);
    }
}
