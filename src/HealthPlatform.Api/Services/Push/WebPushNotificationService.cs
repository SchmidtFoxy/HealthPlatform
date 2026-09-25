using System.Text.Json;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPush;

namespace HealthPlatform.Api.Services.Push;

public sealed class WebPushNotificationService(
    AppDbContext db,
    IOptions<PushOptions> options,
    ILogger<WebPushNotificationService> logger) : IPushNotificationService
{
    public const string LoginProvider = "AESYN.WebPush";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PushOptions _options = options.Value;

    public async Task<PushDispatchResult> EnviarAsync(
        Guid usuarioId,
        string categoria,
        string titulo,
        string mensagem,
        string? link,
        CancellationToken ct = default)
    {
        var usuario = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == usuarioId && x.Ativo, ct);
        if (usuario is null) return new(0, 0, 0, false, "Usuario inativo ou inexistente.");

        if (!PreferenciaPermite(usuario, categoria))
            return new(0, 0, 0, false, "Categoria desativada pelo usuario.");

        var tokens = await db.Set<IdentityUserToken<Guid>>().AsNoTracking()
            .Where(x => x.UserId == usuarioId && x.LoginProvider == LoginProvider)
            .ToListAsync(ct);

        if (tokens.Count == 0) return new(0, 0, 0, _options.Enabled, "Nenhum dispositivo inscrito.");

        // Desenvolvimento/testes ficam deliberadamente sem trafego de push real.
        if (!_options.Enabled)
            return new(tokens.Count, 0, 0, false, "Push desabilitado neste ambiente.");

        if (string.IsNullOrWhiteSpace(_options.Subject) || string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
            return new(tokens.Count, 0, 0, false, "VAPID nao configurado.");

        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        var client = new WebPushClient();
        var payload = JsonSerializer.Serialize(new
        {
            title = titulo,
            body = mensagem,
            icon = "/icons/icon-192.png",
            badge = "/icons/icon-192.png",
            tag = $"aesyn-{categoria}",
            data = new { link = link ?? string.Empty, categoria }
        }, JsonOptions);

        var enviados = 0;
        var falhas = 0;
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token.Value)) continue;
            try
            {
                var item = JsonSerializer.Deserialize<PushSubscriptionData>(token.Value, JsonOptions);
                if (item is null || string.IsNullOrWhiteSpace(item.Endpoint) || string.IsNullOrWhiteSpace(item.P256Dh) || string.IsNullOrWhiteSpace(item.Auth))
                {
                    falhas++;
                    continue;
                }
                var subscription = new PushSubscription(item.Endpoint, item.P256Dh, item.Auth);
                await client.SendNotificationAsync(subscription, payload, vapid);
                enviados++;
            }
            catch (Exception ex)
            {
                falhas++;
                logger.LogWarning(ex, "Falha ao enviar Web Push para usuario {UsuarioId}. A requisicao principal nao sera interrompida.", usuarioId);
            }
        }

        return new(tokens.Count, enviados, falhas, true);
    }

    private static bool PreferenciaPermite(Usuario usuario, string categoria) => categoria.Trim().ToLowerInvariant() switch
    {
        "mensagem" => usuario.NotificarMensagens,
        "plano" => usuario.NotificarAtualizacoesPlano,
        "lembrete" => usuario.NotificarLembretes,
        "checkin" => usuario.NotificarCheckIns,
        _ => true
    };
}

public sealed record PushSubscriptionData(string Endpoint, string P256Dh, string Auth, string? UserAgent, DateTime RegistradaEmUtc);
