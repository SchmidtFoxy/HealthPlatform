using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class CicloEsportivoService
{
    public static async Task<PortalCicloEsportivoResponse?> MontarAtualAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var ciclo = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia && x.DataFim >= dia)
            .OrderByDescending(x => x.DataInicio).FirstOrDefaultAsync(ct);
        if (ciclo is null) return null;

        var totalDias = Math.Max(1, ciclo.DataFim.DayNumber - ciclo.DataInicio.DayNumber + 1);
        var diasDecorridos = Math.Clamp(dia.DayNumber - ciclo.DataInicio.DayNumber + 1, 1, totalDias);
        var totalSemanas = Math.Max(1, (int)Math.Ceiling(totalDias / 7m));
        var semanaAtual = Math.Min(totalSemanas, Math.Max(1, (int)Math.Ceiling(diasDecorridos / 7m)));
        var progresso = Math.Round(diasDecorridos / (decimal)totalDias * 100m, 1);
        var inicioUtc = ciclo.DataInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var treinos = await db.ExecucoesTreino.CountAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido" && x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc, ct);
        var checkinsQuery = db.ProntidoesDiarias.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Data >= ciclo.DataInicio && x.Data <= dia);
        var checkins = await checkinsQuery.CountAsync(ct);
        var media = checkins == 0 ? (decimal?)null : Math.Round(await checkinsQuery.AverageAsync(x => (decimal)x.Score, ct), 1);

        return new PortalCicloEsportivoResponse(ciclo.Id, ciclo.Nome, ciclo.PerfilEsportivo, ciclo.Objetivo, ciclo.DataInicio, ciclo.DataFim, ciclo.Status,
            semanaAtual, totalSemanas, progresso, ciclo.MetaTreinosSemanais, ciclo.MetaConsistenciaPercentual, ciclo.MetaPesoKg, treinos, checkins, media);
    }
}
