using System.Text.Json;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class DorCorporalService
{
    private sealed record DorPayload(string Regiao, string? Lado, int ImpactoTreino, string? Observacao);

    public static async Task<PortalDorCorporalResumoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var fim = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var inicio = fim.AddDays(-7);
        var rows = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "DorCorporal" &&
                        x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .OrderByDescending(x => x.DataHoraUtc)
            .ToListAsync(ct);

        var registros = new List<PortalDorCorporalRegistroResponse>();
        foreach (var x in rows)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<DorPayload>(x.Descricao ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (payload is null || string.IsNullOrWhiteSpace(payload.Regiao)) continue;
                registros.Add(new PortalDorCorporalRegistroResponse(
                    x.Id, DateOnly.FromDateTime(x.DataHoraUtc), payload.Regiao, payload.Lado,
                    x.Escala ?? 0, payload.ImpactoTreino, payload.Observacao));
            }
            catch (JsonException) { }
        }

        var max = registros.Count == 0 ? 0 : registros.Max(x => x.Intensidade);
        var ImpactoMaximoTreino7 = registros.Count == 0 ? 0 : registros.Max(x => x.ImpactoTreino);
        var media = registros.Count == 0 ? (decimal?)null : Math.Round(registros.Average(x => (decimal)x.Intensidade), 1);
        var regioes = registros.Select(x => $"{x.Regiao}|{x.Lado}").Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var nivel = max >= 8 || ImpactoMaximoTreino7 >= 8 ? "Alta" : max >= 5 || ImpactoMaximoTreino7 >= 5 ? "Media" : "Baixa";
        var mensagem = registros.Count == 0
            ? "Nenhuma dor localizada registrada nos últimos 7 dias."
            : nivel == "Alta"
                ? "Há dor localizada ou impacto no treino em intensidade alta; priorize avaliação e não force progressão por conta própria."
                : nivel == "Media"
                    ? "Há desconforto localizado que merece acompanhamento da evolução e relação com os treinos."
                    : "Os registros localizados recentes estão em baixa intensidade.";

        return new PortalDorCorporalResumoResponse(
            registros.Count, regioes, max, media, ImpactoMaximoTreino7, nivel, mensagem, registros.Take(8).ToList());
    }

    public static string Serializar(string regiao, string? lado, int impactoTreino, string? observacao)
        => JsonSerializer.Serialize(new DorPayload(regiao, lado, impactoTreino, observacao));

    public static bool Corresponde(string? descricao, string regiao, string? lado)
    {
        try
        {
            var p = JsonSerializer.Deserialize<DorPayload>(descricao ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return p is not null && string.Equals(p.Regiao, regiao, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(p.Lado ?? "", lado ?? "", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException) { return false; }
    }
}
