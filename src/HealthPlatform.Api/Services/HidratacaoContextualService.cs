using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Resume a execução da hidratação usando metas já prescritas e contexto de treino.
/// Não cria nova meta hídrica; treino e RPE não aumentam automaticamente o volume prescrito.
/// </summary>
public static class HidratacaoContextualService
{
    public static async Task<PortalHidratacaoContextualResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var meta = await db.MetasPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativa" && x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia) &&
                        (x.Tipo.ToLower().Contains("hidrat") || x.Nome.ToLower().Contains("agua") || x.Nome.ToLower().Contains("água") || x.Nome.ToLower().Contains("hidrat")))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (meta is null || !meta.ValorObjetivo.HasValue || meta.ValorObjetivo <= 0)
            return new(null, null, null, null, null, "SemMeta", "Info",
                "Não há meta hídrica ativa para hoje. O sistema não cria uma quantidade automaticamente.",
                Array.Empty<string>());

        var registro = await db.RegistrosMetas.AsNoTracking()
            .Where(x => x.MetaPacienteId == meta.Id && x.Data == dia)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        static decimal ParaMl(decimal valor, string? unidade)
        {
            var u = (unidade ?? string.Empty).Trim().ToLowerInvariant();
            return u is "l" or "litro" or "litros" ? valor * 1000m : valor;
        }

        var metaMl = Math.Round(ParaMl(meta.ValorObjetivo.Value, meta.Unidade), 0);
        decimal? consumidoMl = registro?.Valor is decimal valor ? Math.Round(ParaMl(valor, meta.Unidade), 0) : null;
        decimal? progresso = consumidoMl.HasValue && metaMl > 0
            ? Math.Round(Math.Min(150m, consumidoMl.Value / metaMl * 100m), 1)
            : null;

        var inicioUtc = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);
        var treino = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .Select(x => new { x.DuracaoMinutos, x.EsforcoPercebido })
            .FirstOrDefaultAsync(ct);

        var sinais = new List<string>();
        if (treino?.DuracaoMinutos >= 60) sinais.Add($"Treino de {treino.DuracaoMinutos} min registrado hoje.");
        if (treino?.EsforcoPercebido >= 8) sinais.Add($"Sessão intensa registrada (RPE {treino.EsforcoPercebido}/10).");
        if (progresso >= 100m) sinais.Add("Meta hídrica prescrita atingida hoje.");

        string estado;
        string nivel;
        string mensagem;
        if (!consumidoMl.HasValue)
        {
            estado = "SemRegistro"; nivel = "Info";
            mensagem = "Há uma meta hídrica ativa, mas ainda não há consumo registrado hoje.";
        }
        else if (progresso >= 100m)
        {
            estado = "MetaAtingida"; nivel = "Baixa";
            mensagem = "A meta hídrica prescrita foi atingida. Continue distribuindo a ingestão ao longo do dia sem buscar excesso por pontuação.";
        }
        else
        {
            estado = "EmAndamento"; nivel = treino?.EsforcoPercebido >= 8 || treino?.DuracaoMinutos >= 60 ? "Media" : "Baixa";
            mensagem = "A hidratação está em andamento. Use a meta definida pelo profissional como referência e distribua a ingestão ao longo do dia.";
        }

        return new(metaMl, consumidoMl, progresso, treino?.DuracaoMinutos, treino?.EsforcoPercebido,
            estado, nivel, mensagem, sinais);
    }
}
