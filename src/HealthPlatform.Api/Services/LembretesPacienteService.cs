using System.Text.Json;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Lembretes do paciente (v0.80.0), escolhidos em Perfil › Lembretes.
/// Refeicoes: push no horario de cada refeicao do plano ativo, se ela ainda nao foi marcada no dia.
/// Treino: push no horario escolhido, se hoje tem treino e ele ainda nao foi registrado.
/// Agua continua no lembrete da v0.76.0 (AguaPacienteService) e o chat na preferencia de mensagens da conta.
/// Fica guardado na tabela UsuariosTokens do Identity, como o lembrete de agua: sem SQL.
/// </summary>
public static class LembretesPacienteService
{
    public const string Provedor = "AesynLembretes";
    public const string NomeToken = "Preferencias";
    public static readonly TimeOnly HoraTreinoPadrao = new(18, 0);

    /// <summary>Quanto tempo depois do horario ainda vale mandar (o worker roda a cada 5 min).</summary>
    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(20);

    public sealed record Estado(
        bool Refeicoes,
        bool Treino,
        string HoraTreino,
        int FusoMinutos,
        string? UltimoTreino,
        IReadOnlyList<string>? RefeicoesEnviadas);

    public static Estado Padrao => new(false, false, HoraTreinoPadrao.ToString("HH:mm"), 180, null, null);

    public static Estado Ler(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return Padrao;
        try { return JsonSerializer.Deserialize<Estado>(valor) ?? Padrao; }
        catch (JsonException) { return Padrao; }
    }

    public static TimeOnly HoraTreino(Estado e)
        => TimeOnly.TryParseExact(e.HoraTreino, "HH:mm", out var h) ? h : HoraTreinoPadrao;

    public static async Task<Estado> LerAsync(AppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var token = await db.UserTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == usuarioId && x.LoginProvider == Provedor && x.Name == NomeToken, ct);
        return Ler(token?.Value);
    }

    public static async Task SalvarAsync(AppDbContext db, Guid usuarioId, bool refeicoes, bool treino, TimeOnly horaTreino, int fusoMinutos, CancellationToken ct)
    {
        var token = await db.UserTokens
            .FirstOrDefaultAsync(x => x.UserId == usuarioId && x.LoginProvider == Provedor && x.Name == NomeToken, ct);
        var atual = Ler(token?.Value);
        var valor = JsonSerializer.Serialize(atual with
        {
            Refeicoes = refeicoes,
            Treino = treino,
            HoraTreino = horaTreino.ToString("HH:mm"),
            FusoMinutos = fusoMinutos
        });
        if (token is null)
            db.UserTokens.Add(new IdentityUserToken<Guid> { UserId = usuarioId, LoginProvider = Provedor, Name = NomeToken, Value = valor });
        else
            token.Value = valor;
    }

    /// <summary>Mesma regra da tela de treino: sessao com dias da semana segue a agenda.</summary>
    public static bool SessaoEhDoDia(string? diasSemana, DateOnly dia)
    {
        if (string.IsNullOrWhiteSpace(diasSemana)) return true;
        string[] tokens = dia.DayOfWeek switch
        {
            DayOfWeek.Monday => ["segunda", "seg", "monday", "mon"],
            DayOfWeek.Tuesday => ["terça", "terca", "ter", "tuesday", "tue"],
            DayOfWeek.Wednesday => ["quarta", "qua", "wednesday", "wed"],
            DayOfWeek.Thursday => ["quinta", "qui", "thursday", "thu"],
            DayOfWeek.Friday => ["sexta", "sex", "friday", "fri"],
            DayOfWeek.Saturday => ["sábado", "sabado", "sáb", "sab", "saturday", "sat"],
            _ => ["domingo", "dom", "sunday", "sun"]
        };
        return tokens.Any(t => diasSemana.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public sealed record RefeicaoDoDia(Guid Id, string Nome, TimeOnly? Horario);

    public static async Task<IReadOnlyList<RefeicaoDoDia>> RefeicoesDoPlanoAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var plano = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Refeicoes)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        return plano?.Refeicoes.OrderBy(x => x.Ordem).Select(x => new RefeicaoDoDia(x.Id, x.Nome, x.Horario)).ToList()
               ?? (IReadOnlyList<RefeicaoDoDia>)Array.Empty<RefeicaoDoDia>();
    }

    /// <summary>
    /// Nome do treino que o paciente tem para fazer hoje e ainda nao registrou (null quando nao ha).
    /// Planos com dias da semana seguem a agenda; planos sem dias seguem o rodizio a partir do ultimo treino.
    /// </summary>
    public static async Task<string?> TreinoPendenteHojeAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var plano = await db.PlanosTreino.AsNoTracking()
            .Include(x => x.Sessoes)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (plano is null || plano.Sessoes.Count == 0) return null;

        var inicio = HorarioLocal.InicioUtc(dia);
        var fim = HorarioLocal.FimUtc(dia);
        var feitasHoje = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" && x.DataHoraInicioUtc >= inicio && x.DataHoraInicioUtc < fim)
            .Select(x => x.SessaoTreinoId)
            .ToListAsync(ct);

        var sessoes = plano.Sessoes.OrderBy(x => x.Ordem).ToList();
        if (sessoes.All(x => string.IsNullOrWhiteSpace(x.DiasSemana)))
        {
            if (feitasHoje.Count > 0) return null;
            var ultima = await db.ExecucoesTreino.AsNoTracking()
                .Where(x => x.PacienteId == pacienteId && x.PlanoTreinoId == plano.Id && x.Status == "Concluido")
                .OrderByDescending(x => x.DataHoraInicioUtc)
                .Select(x => (Guid?)x.SessaoTreinoId)
                .FirstOrDefaultAsync(ct);
            var i = sessoes.FindIndex(x => x.Id == ultima);
            return sessoes[(i + 1) % sessoes.Count].Nome;
        }

        return sessoes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.DiasSemana) && SessaoEhDoDia(x.DiasSemana, dia) && !feitasHoje.Contains(x.Id))?.Nome;
    }

    /// <summary>Refeicoes do dia ja marcadas (realizada, adaptada ou nao realizada).</summary>
    public static async Task<HashSet<Guid>> RefeicoesMarcadasAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var inicio = HorarioLocal.InicioUtc(dia);
        var fim = HorarioLocal.FimUtc(dia);
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "AdesaoRefeicao" && x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .Select(x => x.Descricao)
            .ToListAsync(ct);
        return registros.Select(AdesaoNutricionalService.LerStatus).Where(x => x.HasValue).Select(x => x!.Value.RefeicaoId).ToHashSet();
    }
}
