namespace HealthPlatform.Api.Services.Email;

public sealed record EmailDeliveryResult(bool Sent, string Provider, string? Error = null);

public interface ITransactionalEmailService
{
    Task<EmailDeliveryResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}
