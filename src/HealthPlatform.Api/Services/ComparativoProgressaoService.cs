using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Compara janelas temporais equivalentes antes/depois de um evento realmente registrado.
/// Associacao temporal nao implica causalidade.
/// </summary>
public static class ComparativoProgressaoService
{
    public static async Task<PortalComparativoProgressaoResponse> MontarAsync(
        AppDbContext db, Guid organizacaoId, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        const int janelaDias = 7;
        var evento = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataAplicacaoUtc).ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Eixo, x.DataAplicacaoUtc, x.Status })
            .FirstOrDefaultAsync(ct);

        if (evento is null)
            return new("SemMarco", "Comparativo pre/pos ainda indisponivel",
                "E necessario um evento de progressao realmente registrado para criar uma referencia temporal de comparacao.",
                false, null, null, janelaDias, 0, Array.Empty<PortalComparativoProgressaoEixoResponse>(),
                "Sem marco temporal, o sistema nao fabrica um antes/depois.",
                "Sugestao de progressao nao e evento aplicado; sem registro real nao existe comparacao causal ou temporal valida.");

        var marcoUtc = DateTime.SpecifyKind(evento.DataAplicacaoUtc, DateTimeKind.Utc).ToUniversalTime();
        var hojeFimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var antesInicioUtc = marcoUtc.AddDays(-janelaDias);
        var depoisFimPlanejadoUtc = marcoUtc.AddDays(janelaDias);
        var depoisFimUtc = depoisFimPlanejadoUtc < hojeFimUtc ? depoisFimPlanejadoUtc : hojeFimUtc;
        var diasDepois = Math.Max(0, Math.Min(janelaDias, (int)Math.Ceiling((depoisFimUtc - marcoUtc).TotalDays)));

        var dataMarco = DateOnly.FromDateTime(marcoUtc);
        var dataAntesInicio = DateOnly.FromDateTime(antesInicioUtc);
        var dataDepoisFim = DateOnly.FromDateTime(depoisFimUtc.AddTicks(-1));

        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data >= dataAntesInicio && x.Data <= dataDepoisFim)
            .Select(x => new { x.Data, x.Score, x.RecuperacaoNivel, x.DorNivel })
            .ToListAsync(ct);
        var prAntes = prontidoes.Where(x => x.Data < dataMarco).ToList();
        var prDepois = prontidoes.Where(x => x.Data >= dataMarco).ToList();

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= antesInicioUtc && x.DataHoraInicioUtc < depoisFimUtc)
            .Select(x => new { x.Id, x.DataHoraInicioUtc, x.DuracaoMinutos, x.EsforcoPercebido })
            .ToListAsync(ct);
        var trAntes = treinos.Where(x => x.DataHoraInicioUtc < marcoUtc).ToList();
        var trDepois = treinos.Where(x => x.DataHoraInicioUtc >= marcoUtc).ToList();

        var itens = await db.ExecucoesItensTreino.AsNoTracking()
            .Where(x => x.ExecucaoTreino.PacienteId == pacienteId && x.ExecucaoTreino.Status == "Concluido" && x.Concluido &&
                        x.ExecucaoTreino.DataHoraInicioUtc >= antesInicioUtc && x.ExecucaoTreino.DataHoraInicioUtc < depoisFimUtc)
            .Select(x => new { x.ExecucaoTreino.DataHoraInicioUtc, x.CargaRealizada, x.SeriesRealizadas, x.RepeticoesRealizadas })
            .ToListAsync(ct);

        static decimal? Media(IEnumerable<int> valores)
        {
            var l = valores.ToList(); return l.Count == 0 ? null : Math.Round((decimal)l.Average(), 1);
        }
        static decimal? CargaInterna(IEnumerable<(int? duracao, int? rpe)> sessoes)
        {
            var vals = sessoes.Where(x => x.duracao is > 0 && x.rpe is >= 1 and <= 10)
                .Select(x => (decimal)(x.duracao!.Value * x.rpe!.Value)).ToList();
            return vals.Count == 0 ? null : Math.Round(vals.Sum(), 1);
        }
        static decimal? Volume(IEnumerable<(decimal? carga, int? series, string? reps)> linhas)
        {
            decimal total = 0; var tem = false;
            foreach (var x in linhas)
            {
                if (x.carga is null or <= 0 || string.IsNullOrWhiteSpace(x.reps)) continue;
                var nums = Regex.Matches(x.reps, @"\d+").Select(m => int.Parse(m.Value)).ToList();
                if (nums.Count == 0) continue;
                var repsTotal = x.reps.Contains(',') || x.reps.Contains(';') || x.reps.Contains('/')
                    ? nums.Sum() : nums[0] * Math.Max(1, x.series ?? 1);
                total += x.carga.Value * repsTotal; tem = true;
            }
            return tem ? Math.Round(total, 1) : null;
        }
        static decimal? Delta(decimal? a, decimal? d) => a.HasValue && d.HasValue ? Math.Round(d.Value-a.Value,1) : null;
        static string Estado(decimal? a, decimal? d, bool menorMelhor=false)
        {
            if (!a.HasValue || !d.HasValue) return "DadosInsuficientes";
            var dif=d.Value-a.Value; if (Math.Abs(dif) < 0.1m) return "SemMudancaClara";
            return menorMelhor ? (dif < 0 ? "MudancaFavoravel" : "MudancaDesfavoravel") : (dif > 0 ? "MudancaFavoravel" : "MudancaDesfavoravel");
        }

        var prA=Media(prAntes.Select(x=>x.Score)); var prD=Media(prDepois.Select(x=>x.Score));
        var recA=Media(prAntes.Select(x=>x.RecuperacaoNivel)); var recD=Media(prDepois.Select(x=>x.RecuperacaoNivel));
        var dorA=Media(prAntes.Select(x=>x.DorNivel)); var dorD=Media(prDepois.Select(x=>x.DorNivel));
        var cargaA=CargaInterna(trAntes.Select(x=>(x.DuracaoMinutos,x.EsforcoPercebido)));
        var cargaD=CargaInterna(trDepois.Select(x=>(x.DuracaoMinutos,x.EsforcoPercebido)));
        var volA=Volume(itens.Where(x=>x.DataHoraInicioUtc<marcoUtc).Select(x=>(x.CargaRealizada,x.SeriesRealizadas,x.RepeticoesRealizadas)));
        var volD=Volume(itens.Where(x=>x.DataHoraInicioUtc>=marcoUtc).Select(x=>(x.CargaRealizada,x.SeriesRealizadas,x.RepeticoesRealizadas)));

        var eixos = new List<PortalComparativoProgressaoEixoResponse>
        {
            new("prontidao","Prontidao media","/100",prA,prD,Delta(prA,prD),prAntes.Count,prDepois.Count,Estado(prA,prD),"Media dos check-ins dentro de janelas temporais equivalentes."),
            new("recuperacao","Recuperacao percebida","/10",recA,recD,Delta(recA,recD),prAntes.Count,prDepois.Count,Estado(recA,recD),"Recuperacao autorreferida no diario de prontidao."),
            new("dor","Dor media","/10",dorA,dorD,Delta(dorA,dorD),prAntes.Count,prDepois.Count,Estado(dorA,dorD,true),"Dor e contexto clinico devem ser interpretados separadamente de performance."),
            new("carga","Carga interna","u.a.",cargaA,cargaD,Delta(cargaA,cargaD),trAntes.Count,trDepois.Count,"Contexto","Duracao x RPE; maior ou menor nao e automaticamente melhor ou pior."),
            new("volume","Volume estimado","kg-reps",volA,volD,Delta(volA,volD),itens.Count(x=>x.DataHoraInicioUtc<marcoUtc),itens.Count(x=>x.DataHoraInicioUtc>=marcoUtc),"Contexto","Volume estimado das execucoes com carga e repeticoes registradas.")
        };

        var suficientes = eixos.Count(x => x.Antes.HasValue && x.Depois.HasValue);
        var estado = diasDepois < 3 || suficientes < 2 ? "DadosInsuficientes" : diasDepois < janelaDias ? "JanelaEmFormacao" : "Comparavel";
        var resumo = estado switch
        {
            "DadosInsuficientes" => "Ainda ha pouco tempo ou poucos registros apos o evento para um comparativo util.",
            "JanelaEmFormacao" => "O comparativo ja mostra sinais iniciais, mas a janela pos-progressao ainda esta em formacao.",
            _ => "Ha janelas temporais comparaveis antes e depois do evento registrado."
        };
        return new(estado, "Comparativo pre/pos-progressao", resumo, true, evento.Eixo, marcoUtc, janelaDias, diasDepois, eixos,
            "Leia cada eixo separadamente e junto do ciclo, sintomas e contexto profissional; nao transforme diferenca temporal em conclusao causal.",
            "Associacao temporal nao implica causalidade. O comparativo nao prova beneficio, dano ou resposta causada pela progressao e nao autoriza nova mudanca automatica.");
    }
}
