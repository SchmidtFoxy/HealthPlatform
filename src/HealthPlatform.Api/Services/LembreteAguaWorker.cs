using HealthPlatform.Api.Services.Push;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Lembrete de agua por push (v0.74.0). So para quem ligou o lembrete no card de agua.
/// Nos horarios fixos (10h, 13h, 16h e 19h no fuso do aparelho), manda um push se o paciente
/// estiver abaixo do ritmo do dia. Quem esta em dia nao recebe nada. No maximo um por horario.
/// Liga e desliga no sino do card de agua ou em Perfil › Lembretes (categoria "agua", v0.80.0).
/// </summary>
public sealed class LembreteAguaWorker(IServiceScopeFactory scopes, ILogger<LembreteAguaWorker> logger) : BackgroundService
{
    private static readonly int[] Horarios = [10, 13, 16, 19];
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera a API subir (migrations, seed) antes da primeira rodada.
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
                logger.LogWarning(ex, "Falha na rodada do lembrete de agua. Tenta de novo na proxima.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RodarAsync(DateTime agoraUtc, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var tokens = await db.UserTokens
            .Where(x => x.LoginProvider == AguaPacienteService.Provedor && x.Name == AguaPacienteService.NomeToken)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            var estado = AguaPacienteService.Ler(token.Value);
            if (estado is not { Ativo: true }) continue;

            var local = agoraUtc.AddMinutes(-estado.FusoMinutos);
            if (!Horarios.Contains(local.Hour)) continue;
            var slot = $"{local:yyyy-MM-dd}T{local.Hour:00}";
            if (estado.UltimoEnvio == slot) continue;

            var pacienteId = await db.Pacientes.AsNoTracking()
                .Where(x => x.UsuarioId == token.UserId && x.Ativo)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);

            // Marca o horario antes de enviar: se o envio falhar, nao insiste ate o proximo horario.
            token.Value = System.Text.Json.JsonSerializer.Serialize(estado with { UltimoEnvio = slot });
            await db.SaveChangesAsync(ct);
            if (!pacienteId.HasValue) continue;

            var hoje = DateOnly.FromDateTime(local);
            var resumo = await AguaPacienteService.MontarAsync(db, pacienteId.Value, token.UserId, hoje, estado.FusoMinutos, ct);
            var esperado = AguaPacienteService.RitmoEsperadoMl(resumo.MetaMl, TimeOnly.FromDateTime(local));
            if (resumo.HojeMl >= resumo.MetaMl || resumo.HojeMl >= esperado * 0.85m) continue;

            var falta = esperado - resumo.HojeMl;
            var copos = Math.Max(1, (int)Math.Ceiling(falta / 250m));
            var mensagem = resumo.HojeMl <= 0
                ? $"Ainda não tem água registrada hoje. Que tal começar com um copo? Meta: {Litros(resumo.MetaMl)}."
                : $"Você está em {Litros(resumo.HojeMl)} de {Litros(resumo.MetaMl)}. {(copos == 1 ? "Um copo" : $"{copos} copos")} agora te deixa no ritmo.";
            await push.EnviarAsync(token.UserId, "agua", "Hora da água 💧", mensagem, "inicio", ct);
        }
    }

    private static string Litros(decimal ml) => ml >= 1000 ? $"{ml / 1000m:0.##} L".Replace('.', ',') : $"{ml:0} ml";
}
