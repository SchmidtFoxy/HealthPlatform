using System.Text.Json;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class EventoDesvioAdesaoService
{
    public const string TipoRegistro = "EventoDesvioAdesao";

    public static async Task RegistrarAsync(
        AppDbContext db,
        Guid pacienteId,
        DateTime dataHoraUtc,
        string categoria,
        string tipo,
        string titulo,
        string detalhe,
        string origemChave,
        int prioridade,
        object? contexto,
        CancellationToken ct)
    {
        prioridade = Math.Clamp(prioridade, 1, 3);
        var inicio = dataHoraUtc.Date;
        var fim = inicio.AddDays(1);

        var existentes = await db.RegistrosDiarioPaciente
            .Where(x => x.PacienteId == pacienteId && x.Tipo == TipoRegistro &&
                        x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .Select(x => x.Descricao)
            .ToListAsync(ct);

        if (existentes.Any(x => ContemChave(x, origemChave)))
            return;

        var payload = new EventoDesvioAdesaoPayload(
            categoria,
            tipo,
            titulo,
            detalhe,
            origemChave,
            prioridade,
            dataHoraUtc,
            contexto);

        db.RegistrosDiarioPaciente.Add(new RegistroDiarioPaciente
        {
            PacienteId = pacienteId,
            DataHoraUtc = dataHoraUtc,
            Tipo = TipoRegistro,
            Descricao = JsonSerializer.Serialize(payload),
            ValorNumerico = 1m,
            Unidade = categoria,
            Escala = prioridade,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    public static EventoDesvioAdesaoLeitura? Ler(RegistroDiarioPaciente item)
    {
        if (!string.Equals(item.Tipo, TipoRegistro, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(item.Descricao))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<EventoDesvioAdesaoPayload>(item.Descricao);
            if (payload is null) return null;
            return new EventoDesvioAdesaoLeitura(
                item.Id,
                payload.Categoria,
                payload.Tipo,
                payload.Titulo,
                payload.Detalhe,
                payload.OrigemChave,
                payload.Prioridade,
                payload.DataHoraUtc,
                payload.Contexto);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool ContemChave(string? descricao, string chave)
    {
        if (string.IsNullOrWhiteSpace(descricao)) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<EventoDesvioAdesaoPayload>(descricao);
            return string.Equals(payload?.OrigemChave, chave, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return descricao.Contains(chave, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed record EventoDesvioAdesaoPayload(
        string Categoria,
        string Tipo,
        string Titulo,
        string Detalhe,
        string OrigemChave,
        int Prioridade,
        DateTime DataHoraUtc,
        object? Contexto);
}

public sealed record EventoDesvioAdesaoLeitura(
    Guid Id,
    string Categoria,
    string Tipo,
    string Titulo,
    string Detalhe,
    string OrigemChave,
    int Prioridade,
    DateTime DataHoraUtc,
    object? Contexto);
