namespace HealthPlatform.Api.Services.Push;

public sealed record PushDispatchResult(int Registradas, int Enviadas, int Falhas, bool Habilitado, string? Motivo = null);

public interface IPushNotificationService
{
    Task<PushDispatchResult> EnviarAsync(
        Guid usuarioId,
        string categoria,
        string titulo,
        string mensagem,
        string? link,
        CancellationToken ct = default);
}
