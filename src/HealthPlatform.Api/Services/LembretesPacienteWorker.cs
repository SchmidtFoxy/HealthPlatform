using System.Text.Json;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Lembretes de refeicao e de treino por push (v0.80.0). So para quem ligou em Perfil › Lembretes.
/// Refeicao: no horario de cada refeicao do plano, se ela ainda nao foi marcada hoje.
/// Treino: no horario escolhido, se hoje tem treino e ele ainda nao foi registrado.
/// Cada lembrete sai no maximo uma vez por dia, dentro de 20 min do horario (API fora do ar nao gera rajada depois).
/// </summary>
public sealed class LembretesPacienteWorker(IServiceScopeFactory scopes, ILogger<LembretesPacienteWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await RodarAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha na rodada dos lembretes do paciente. Tenta de novo na proxima.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RodarAsync(DateTime agoraUtc, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var tokens = await db.UserTokens
            .Where(x => x.LoginProvider == LembretesPacienteService.Provedor && x.Name == LembretesPacienteService.NomeToken)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            var estado = LembretesPacienteService.Ler(token.Value);
            if (!estado.Refeicoes && !estado.Treino) continue;

            var local = agoraUtc.AddMinutes(-estado.FusoMinutos);
            var hoje = DateOnly.FromDateTime(local);
            var agora = TimeOnly.FromDateTime(local);
            var chaveHoje = hoje.ToString("yyyy-MM-dd");

            var pacienteId = await db.Pacientes.AsNoTracking()
                .Where(x => x.UsuarioId == token.UserId && x.Ativo)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);
            if (!pacienteId.HasValue) continue;

            var enviadas = (estado.RefeicoesEnviadas ?? []).Where(x => x.StartsWith(chaveHoje + "|", StringComparison.Ordinal)).ToList();
            var envios = new List<(string Titulo, string Mensagem, string Link)>();

            if (estado.Refeicoes)
            {
                var refeicoes = await LembretesPacienteService.RefeicoesDoPlanoAsync(db, pacienteId.Value, hoje, ct);
                var naHora = refeicoes.Where(r => r.Horario is { } h && NaJanela(agora, h) && !enviadas.Contains($"{chaveHoje}|{r.Id}")).ToList();
                if (naHora.Count > 0)
                {
                    var marcadas = await LembretesPacienteService.RefeicoesMarcadasAsync(db, pacienteId.Value, hoje, ct);
                    foreach (var r in naHora)
                    {
                        // Marca antes de enviar: se o envio falhar, nao insiste.
                        enviadas.Add($"{chaveHoje}|{r.Id}");
                        if (marcadas.Contains(r.Id)) continue;
                        envios.Add(($"Hora do {r.Nome.Trim()} 🍽️", "Seu plano está no app. Depois é só marcar como foi.", "alimentacao"));
                    }
                }
            }

            var ultimoTreino = estado.UltimoTreino;
            if (estado.Treino && ultimoTreino != chaveHoje && NaJanela(agora, LembretesPacienteService.HoraTreino(estado)))
            {
                ultimoTreino = chaveHoje;
                var treino = await LembretesPacienteService.TreinoPendenteHojeAsync(db, pacienteId.Value, hoje, ct);
                if (treino is not null)
                    envios.Add(("Hoje tem treino 💪", $"{treino.Trim()} está te esperando. Bora?", "treinos"));
            }

            var novo = estado with { RefeicoesEnviadas = enviadas, UltimoTreino = ultimoTreino };
            if (novo.UltimoTreino == estado.UltimoTreino && enviadas.SequenceEqual(estado.RefeicoesEnviadas ?? [])) continue;
            token.Value = JsonSerializer.Serialize(novo);
            await db.SaveChangesAsync(ct);

            foreach (var (titulo, mensagem, link) in envios)
                await push.EnviarAsync(token.UserId, link == "treinos" ? "treino" : "refeicao", titulo, mensagem, link, ct);
        }
    }

    private static bool NaJanela(TimeOnly agora, TimeOnly horario)
    {
        var diff = agora.ToTimeSpan() - horario.ToTimeSpan();
        return diff >= TimeSpan.Zero && diff < LembretesPacienteService.Janela;
    }
}
