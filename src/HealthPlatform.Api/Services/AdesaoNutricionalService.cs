using System.Text.Json;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class AdesaoNutricionalService
{
    private sealed record RegistroPayload(Guid RefeicaoId, string Status, string? Observacao);

    public static async Task<PortalAdesaoNutricionalResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var plano = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Refeicoes)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (plano is null)
            return new(0, 0, 0, 0, 0, null, "SemPlano",
                "Não há plano alimentar ativo para este dia.", Array.Empty<PortalAdesaoNutricionalRefeicaoResponse>());

        var inicio = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "AdesaoRefeicao" &&
                        x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .OrderByDescending(x => x.DataHoraUtc)
            .ToListAsync(ct);

        var porRefeicao = new Dictionary<Guid, RegistroPayload>();
        foreach (var registro in registros)
        {
            var payload = Ler(registro.Descricao);
            if (payload is not null && !porRefeicao.ContainsKey(payload.RefeicaoId))
                porRefeicao[payload.RefeicaoId] = payload;
        }

        var refeicoes = plano.Refeicoes.OrderBy(x => x.Ordem).Select(x =>
        {
            porRefeicao.TryGetValue(x.Id, out var registro);
            return new PortalAdesaoNutricionalRefeicaoResponse(
                x.Id, x.Nome, x.Horario, registro?.Status ?? "Pendente", registro?.Observacao);
        }).ToList();

        var registrados = refeicoes.Where(x => x.Status != "Pendente").ToList();
        var realizadas = registrados.Count(x => x.Status == "Realizada");
        var adaptadas = registrados.Count(x => x.Status == "Adaptada");
        var naoRealizadas = registrados.Count(x => x.Status == "NaoRealizada");
        decimal? adequacao = registrados.Count == 0 ? null : Math.Round(
            registrados.Average(x => x.Status switch { "Realizada" => 100m, "Adaptada" => 80m, _ => 0m }), 1);

        var estado = registrados.Count == 0 ? "SemRegistros"
            : registrados.Count < refeicoes.Count ? "EmAndamento"
            : adequacao >= 80m ? "BoaAdesao"
            : adequacao >= 60m ? "Parcial" : "Revisar";

        var mensagem = estado switch
        {
            "SemRegistros" => "Registre como as refeições aconteceram hoje; o objetivo é reconhecer padrões, não buscar perfeição.",
            "EmAndamento" => $"{registrados.Count}/{refeicoes.Count} refeição(ões) registradas. Continue acompanhando sem compensações fora do plano.",
            "BoaAdesao" => "Boa execução do plano registrado hoje. Preserve consistência sem transformar alimentação em meta de perfeição.",
            "Parcial" => "O dia teve adaptações. Use o registro para entender contexto e manter o plano sustentável.",
            _ => "Houve refeições não realizadas ou baixa adequação registrada. Observe o contexto e leve padrões recorrentes ao profissional."
        };

        return new(refeicoes.Count, registrados.Count, realizadas, adaptadas, naoRealizadas,
            adequacao, estado, mensagem, refeicoes);
    }

    public static string Serializar(Guid refeicaoId, string status, string? observacao)
        => JsonSerializer.Serialize(new { refeicaoId, status = NormalizarStatus(status), observacao = Limpar(observacao) });

    public static bool Corresponde(string? descricao, Guid refeicaoId)
        => Ler(descricao)?.RefeicaoId == refeicaoId;

    public static string NormalizarStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "realizada" => "Realizada",
        "adaptada" => "Adaptada",
        "naorealizada" or "nao-realizada" or "não realizada" or "nao realizada" => "NaoRealizada",
        _ => string.Empty
    };

    private static RegistroPayload? Ler(string? descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao)) return null;
        try
        {
            using var doc = JsonDocument.Parse(descricao);
            var root = doc.RootElement;
            if (!root.TryGetProperty("refeicaoId", out var idEl) || !Guid.TryParse(idEl.GetString(), out var refeicaoId)) return null;
            if (!root.TryGetProperty("status", out var statusEl)) return null;
            var status = NormalizarStatus(statusEl.GetString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(status)) return null;
            string? observacao = null;
            if (root.TryGetProperty("observacao", out var obsEl) && obsEl.ValueKind != JsonValueKind.Null) observacao = Limpar(obsEl.GetString());
            return new RegistroPayload(refeicaoId, status, observacao);
        }
        catch { return null; }
    }

    private static string? Limpar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
