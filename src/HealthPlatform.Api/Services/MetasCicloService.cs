using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class MetasCicloService
{
    public static async Task<PortalMetasCicloResponse> MontarAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        PortalCicloEsportivoResponse? ciclo,
        PortalGamificacaoResponse gamificacao,
        CancellationToken ct)
    {
        if (ciclo is null)
            return new("SemCiclo", "Metas do ciclo", "Nenhum ciclo esportivo ativo neste momento.",
                Array.Empty<PortalMetaCicloItemResponse>(),
                "As metas são definidas pelo profissional; o sistema apenas acompanha o progresso registrado.");

        var itens = new List<PortalMetaCicloItemResponse>();

        if (ciclo.MetaTreinosSemanais.HasValue)
        {
            var inicioSemana = InicioDaSemana(dia);
            if (inicioSemana < ciclo.DataInicio) inicioSemana = ciclo.DataInicio;
            var inicioUtc = inicioSemana.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var treinosSemana = await db.ExecucoesTreino.AsNoTracking()
                .CountAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                                 x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc, ct);
            var meta = ciclo.MetaTreinosSemanais.Value;
            var progresso = Percentual(treinosSemana, meta);
            itens.Add(new("treinos-semana", "Treinos da semana", EstadoMeta(progresso),
                $"{treinosSemana} treino(s)", $"{meta} treino(s)", progresso,
                "Conta somente sessões concluídas na semana atual do ciclo."));
        }

        if (ciclo.MetaConsistenciaPercentual.HasValue)
        {
            var meta = ciclo.MetaConsistenciaPercentual.Value;
            var progresso = meta <= 0 ? 100m : Math.Round(Math.Clamp(gamificacao.ConsistenciaScore / (decimal)meta * 100m, 0m, 100m), 1);
            itens.Add(new("consistencia", "Consistência", EstadoMeta(progresso),
                $"{gamificacao.ConsistenciaScore}/100", $"{meta}/100", progresso,
                "Compara a consistência sustentável atual com a meta definida para o ciclo."));
        }

        if (ciclo.MetaPesoKg.HasValue)
        {
            var inicioCicloUtc = ciclo.DataInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var fimDiaUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var pesos = await db.Avaliacoes.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.PesoKg.HasValue && x.DataUtc >= inicioCicloUtc && x.DataUtc < fimDiaUtc)
                .OrderBy(x => x.DataUtc)
                .Select(x => new { x.DataUtc, Peso = x.PesoKg!.Value })
                .ToListAsync(ct);

            if (pesos.Count == 0)
            {
                itens.Add(new("peso-alvo", "Peso-alvo", "DadosInsuficientes", "Sem avaliação", $"{ciclo.MetaPesoKg:0.0} kg", null,
                    "Registre uma avaliação corporal no ciclo para acompanhar a distância até o peso-alvo."));
            }
            else
            {
                var inicial = pesos[0].Peso;
                var atual = pesos[^1].Peso;
                var alvo = ciclo.MetaPesoKg.Value;
                var distanciaInicial = Math.Abs(inicial - alvo);
                var distanciaAtual = Math.Abs(atual - alvo);
                decimal progresso;
                if (distanciaInicial <= 0.1m) progresso = 100m;
                else progresso = Math.Round(Math.Clamp((distanciaInicial - distanciaAtual) / distanciaInicial * 100m, 0m, 100m), 1);
                var estado = distanciaAtual <= 0.3m ? "Atingida" : distanciaAtual > distanciaInicial + 0.3m ? "Observar" : EstadoMeta(progresso);
                itens.Add(new("peso-alvo", "Peso-alvo", estado, $"{atual:0.0} kg", $"{alvo:0.0} kg", progresso,
                    $"Início do ciclo: {inicial:0.0} kg • distância atual: {distanciaAtual:0.0} kg."));
            }
        }

        if (!string.IsNullOrWhiteSpace(ciclo.Objetivo))
            itens.Add(new("objetivo-profissional", "Objetivo do ciclo", "Informativo", ciclo.Objetivo!, "Estratégia profissional", null,
                "Objetivo textual definido pelo profissional; não é convertido automaticamente em score."));

        var numericas = itens.Where(x => x.ProgressoPercentual.HasValue).ToList();
        var atingidas = numericas.Count(x => x.Estado == "Atingida");
        var observar = itens.Count(x => x.Estado == "Observar");
        var estadoGeral = itens.Count == 0 ? "SemMetas" : observar > 0 ? "Observar" : atingidas == numericas.Count && numericas.Count > 0 ? "Atingindo" : "EmCurso";
        var resumo = itens.Count == 0
            ? "O ciclo atual ainda não possui metas mensuráveis configuradas."
            : observar > 0
                ? "Há uma meta pedindo revisão de contexto; veja cada indicador antes de ajustar a estratégia."
                : $"{atingidas}/{numericas.Count} meta(s) mensurável(is) atingida(s) no momento.";

        return new(estadoGeral, "Progresso das metas do ciclo", resumo, itens,
            "Progresso não autoriza aumento de carga, restrição alimentar ou mudança de prescrição sem revisão profissional.");
    }

    private static decimal Percentual(decimal atual, decimal meta) => meta <= 0 ? 100m : Math.Round(Math.Clamp(atual / meta * 100m, 0m, 100m), 1);
    private static string EstadoMeta(decimal progresso) => progresso >= 100m ? "Atingida" : progresso >= 60m ? "EmCurso" : "Construindo";
    private static DateOnly InicioDaSemana(DateOnly dia)
    {
        var delta = ((int)dia.DayOfWeek + 6) % 7;
        return dia.AddDays(-delta);
    }
}
