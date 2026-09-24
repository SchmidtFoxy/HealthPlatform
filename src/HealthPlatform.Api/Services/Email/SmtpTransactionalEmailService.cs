using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace HealthPlatform.Api.Services.Email;

public sealed class SmtpTransactionalEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpTransactionalEmailService> logger) : ITransactionalEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailDeliveryResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled)
        {
            logger.LogInformation("Email transactional skipped because Email:Enabled=false. To={To} Subject={Subject}", to, subject);
            return new EmailDeliveryResult(false, "disabled");
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            return new EmailDeliveryResult(false, "smtp", "Configuracao SMTP incompleta.");

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(to));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
                client.Credentials = new NetworkCredential(_options.Username, _options.Password ?? string.Empty);

            await client.SendMailAsync(message);
            return new EmailDeliveryResult(true, "smtp");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transactional email delivery failed. To={To} Subject={Subject}", to, subject);
            return new EmailDeliveryResult(false, "smtp", "Falha no envio do email.");
        }
    }
}
