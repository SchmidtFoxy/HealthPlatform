using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class CargaIndividualizadaService
{
    public static async Task<PortalCargaIndividualizadaResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia,
        PortalCargaTreinoResponse cargaAtual,
        PortalTendenciaRecuperacaoResponse recuperacao,
        PortalRespostaSessaoResponse respostaSessao,
        CancellationToken ct)
    {
        const int diasObservados = 56;
        var inicioAtual = dia.AddDays(-6);
        var inicioHistorico = inicioAtual.AddDays(-49);
        var inicioUtc = DateTime.SpecifyKind(inicioHistorico.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .Select(x => new { x.DataHoraInicioUtc, x.DuracaoMinutos, x.EsforcoPercebido })
            .ToListAsync(ct);

        static decimal CargaSessao(int? duracao, int? rpe)
            => duracao is > 0 && rpe is >= 1 and <= 10 ? duracao.Value * rpe.Value : 0m;

        var semanas = new List<PortalCargaIndividualizadaSemanaResponse>();
        // Sete blocos completos anteriores a janela atual. A referencia vem do proprio atleta.
        for (var i = 7; i >= 1; i--)
        {
            var inicio = inicioAtual.AddDays(-(i * 7));
            var fim = inicio.AddDays(6);
            var inicioSemanaUtc = DateTime.SpecifyKind(inicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var fimSemanaUtc = DateTime.SpecifyKind(fim.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var itens = treinos.Where(x => x.DataHoraInicioUtc >= inicioSemanaUtc && x.DataHoraInicioUtc < fimSemanaUtc).ToList();
            var cargas = itens.Select(x => CargaSessao(x.DuracaoMinutos, x.EsforcoPercebido)).Where(x => x > 0m).ToList();
            var rpes = itens.Where(x => x.EsforcoPercebido.HasValue).Select(x => x.EsforcoPercebido!.Value).ToList();
            semanas.Add(new(
                inicio, fim, itens.Count, Math.Round(cargas.Sum(), 1),
                rpes.Count > 0 ? Math.Round((decimal)rpes.Average(), 1) : null,
                itens.Sum(x => x.DuracaoMinutos ?? 0)));
        }

        static decimal Percentil(IReadOnlyList<decimal> valoresOrdenados, decimal p)
        {
            if (valoresOrdenados.Count == 1) return valoresOrdenados[0];
            var pos = (valoresOrdenados.Count - 1) * p;
            var abaixo = (int)Math.Floor(pos);
            var acima = (int)Math.Ceiling(pos);
            if (abaixo == acima) return valoresOrdenados[abaixo];
            var peso = pos - abaixo;
            return valoresOrdenados[abaixo] + ((valoresOrdenados[acima] - valoresOrdenados[abaixo]) * peso);
        }

        var cargasAtivas = semanas.Where(x => x.CargaInterna > 0m).Select(x => x.CargaInterna).OrderBy(x => x).ToList();
        decimal? mediana = cargasAtivas.Count >= 1 ? Math.Round(Percentil(cargasAtivas, 0.50m), 1) : null;
        decimal? q25 = cargasAtivas.Count >= 4 ? Math.Round(Percentil(cargasAtivas, 0.25m), 1) : null;
        decimal? q75 = cargasAtivas.Count >= 4 ? Math.Round(Percentil(cargasAtivas, 0.75m), 1) : null;
        var carga7 = cargaAtual.CargaInterna7;

        string posicaoHistorica;
        if (!carga7.HasValue) posicaoHistorica = "SemCargaAtual";
        else if (cargasAtivas.Count < 4 || !q25.HasValue || !q75.HasValue) posicaoHistorica = "ReferenciaEmConstrucao";
        else if (carga7.Value < q25.Value) posicaoHistorica = "AbaixoDoHistoricoRecente";
        else if (carga7.Value > q75.Value) posicaoHistorica = "AcimaDoHistoricoRecente";
        else posicaoHistorica = "DentroDoHistoricoRecente";

        var contextoRecuperacao = recuperacao.Tendencia;
        var contextoResposta = respostaSessao.Estado;
        var sinais = new List<string>();
        if (posicaoHistorica == "AcimaDoHistoricoRecente") sinais.Add("Carga atual acima do quartil superior do historico recente do proprio atleta.");
        if (posicaoHistorica == "AbaixoDoHistoricoRecente") sinais.Add("Carga atual abaixo do quartil inferior do historico recente do proprio atleta.");
        if (recuperacao.Tendencia == "Atencao") sinais.Add("Recuperacao recente pede revisao antes de interpretar aumento de carga como adaptacao positiva.");
        else if (recuperacao.Tendencia == "Observar") sinais.Add("Recuperacao recente apresenta oscilacao e deve acompanhar a leitura de carga.");
        if (respostaSessao.Estado == "Revisar") sinais.Add("Resposta a sessao recente tem eixo que pede revisao.");
        else if (respostaSessao.Estado == "Observar") sinais.Add("Resposta a sessao recente merece acompanhamento antes de nova mudanca.");

        string estado;
        if (!carga7.HasValue) estado = "DadosInsuficientes";
        else if (cargasAtivas.Count < 4) estado = "ConstruindoReferencia";
        else if (recuperacao.Tendencia == "Atencao" || respostaSessao.Estado == "Revisar") estado = "RevisarContexto";
        else if (recuperacao.Tendencia == "Observar" || respostaSessao.Estado == "Observar") estado = "ObservarContexto";
        else estado = "ReferenciaIndividualDisponivel";

        var resumo = estado switch
        {
            "DadosInsuficientes" => "Ainda faltam sessoes com duracao e RPE para interpretar a carga atual.",
            "ConstruindoReferencia" => "A carga atual ja pode ser vista, mas o historico individual ainda nao tem semanas ativas suficientes para uma referencia robusta.",
            "RevisarContexto" => "A posicao da carga deve ser interpretada junto de sinais atuais de recuperacao ou resposta a sessao antes de qualquer progressao.",
            "ObservarContexto" => "Existe referencia individual, mas a resposta recente pede observacao antes de mudar novamente a estrategia.",
            _ => "A carga atual pode ser comparada com a distribuicao recente do proprio atleta; isso descreve o contexto e nao define uma zona ideal."
        };

        var leitura = posicaoHistorica switch
        {
            "AcimaDoHistoricoRecente" => "A carga dos ultimos 7 dias esta acima da faixa central observada nas semanas ativas anteriores. Isso nao significa excesso por si so.",
            "AbaixoDoHistoricoRecente" => "A carga dos ultimos 7 dias esta abaixo da faixa central observada nas semanas ativas anteriores e pode refletir deload, recuperacao ou mudanca planejada.",
            "DentroDoHistoricoRecente" => "A carga dos ultimos 7 dias esta dentro da faixa central observada no proprio historico recente.",
            "ReferenciaEmConstrucao" => "O historico ainda esta sendo formado; a plataforma evita criar limites individuais precoces.",
            _ => "Sem carga atual suficiente para posicionamento historico."
        };

        return new PortalCargaIndividualizadaResponse(
            estado, posicaoHistorica, "Carga esportiva individualizada", resumo, diasObservados,
            semanas.Count, cargasAtivas.Count, carga7, mediana, q25, q75,
            contextoRecuperacao, contextoResposta, semanas, sinais, leitura,
            "Referencia individual e descritiva: nao e zona segura, nao estima risco de lesao, nao diagnostica excesso e nao prescreve aumento ou reducao automatica de carga.");
    }
}
