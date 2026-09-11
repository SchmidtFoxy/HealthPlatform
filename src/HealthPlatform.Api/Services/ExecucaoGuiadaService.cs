using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class ExecucaoGuiadaService
{
    public static async Task<PortalExecucaoDiaResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, PortalEstrategiaDiaResponse estrategia, CancellationToken ct)
    {
        var inicio = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var itens = new List<PortalExecucaoDiaItemResponse>();

        var prontidao = await db.ProntidoesDiarias.AsNoTracking()
            .AnyAsync(x => x.PacienteId == pacienteId && x.Data == dia, ct);
        itens.Add(new("prontidao", "Autocuidado", "Check-in de prontidão",
            "Sono, energia, dor, disposição e recuperação orientam a carga do dia.",
            prontidao ? "Concluido" : "Pendente", true, prontidao ? 100m : 0m,
            prontidao ? "registrado" : null, "1 check-in", "prontidao"));

        var treinoHoje = await db.ExecucoesTreino.AsNoTracking()
            .AnyAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                           x.DataHoraInicioUtc >= inicio && x.DataHoraInicioUtc < fim, ct);
        if (estrategia.SessoesPrevistas.Count > 0)
        {
            var recuperacao = estrategia.IntensidadeSugerida == "Recuperacao";
            itens.Add(new("treino", "Treino", recuperacao ? "Recuperação / sessão adaptada" : "Treino do dia",
                recuperacao
                    ? "Se o plano permitir, priorize recuperação ativa ou a sessão adaptada pela estratégia do dia."
                    : $"Sessão prevista: {string.Join(" / ", estrategia.SessoesPrevistas.Take(2))}.",
                treinoHoje ? "Concluido" : recuperacao ? "Opcional" : "Pendente", !recuperacao,
                treinoHoje ? 100m : 0m, treinoHoje ? "concluído" : null,
                recuperacao ? "recuperação adequada" : "1 sessão", "treino"));
        }

        var metas = await db.MetasPaciente.AsNoTracking().Include(x => x.Registros)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativa" && x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderBy(x => x.Nome).ToListAsync(ct);

        foreach (var meta in metas)
        {
            var reg = meta.Registros.FirstOrDefault(x => x.Data == dia);
            var pct = reg?.Concluida == true ? 100m :
                meta.ValorObjetivo.HasValue && meta.ValorObjetivo > 0 && reg?.Valor is not null
                    ? Math.Round(Math.Clamp(reg.Valor.Value / meta.ValorObjetivo.Value * 100m, 0m, 100m), 1) : 0m;
            var categoria = EhHidratacao(meta.Nome, meta.Tipo) ? "Hidratacao" : "Meta";
            itens.Add(new($"meta:{meta.Id}", categoria, meta.Nome,
                categoria == "Hidratacao" ? "Distribua a hidratação ao longo do dia; não concentre tudo no fim." : "Meta definida no acompanhamento profissional.",
                reg?.Concluida == true ? "Concluido" : "Pendente", true, pct,
                reg?.Valor?.ToString("0.##"), meta.ValorObjetivo.HasValue ? $"{meta.ValorObjetivo.Value:0.##} {meta.Unidade}".Trim() : null,
                $"meta:{meta.Id}"));
        }

        var fechamento = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "FechamentoDia" && x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .OrderByDescending(x => x.DataHoraUtc).FirstOrDefaultAsync(ct);
        itens.Add(new("fechamento", "Reflexao", "Fechar o dia",
            "Registre como o dia terminou. Isso ajuda a comparar plano, execução e recuperação amanhã.",
            fechamento is null ? "Pendente" : "Concluido", true, fechamento is null ? 0m : 100m,
            fechamento?.Escala is null ? null : $"{fechamento.Escala}/10", "1 reflexão", "fechamento"));

        var obrigatorios = itens.Where(x => x.Obrigatorio).ToList();
        var concluidos = obrigatorios.Count(x => x.Status == "Concluido");
        var progresso = obrigatorios.Count == 0 ? 100m : Math.Round((decimal)concluidos / obrigatorios.Count * 100m, 1);
        return new PortalExecucaoDiaResponse(itens.Count, concluidos, obrigatorios.Count - concluidos, progresso,
            fechamento is not null, fechamento?.Escala, fechamento?.Descricao, itens);
    }

    private static bool EhHidratacao(string nome, string tipo)
    {
        var texto = $"{nome} {tipo}".ToLowerInvariant();
        return texto.Contains("agua") || texto.Contains("água") || texto.Contains("hidrat");
    }
}
