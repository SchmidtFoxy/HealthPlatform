using System.Net;

namespace HealthPlatform.Api.Services.Email;

public static class AccountEmailTemplates
{
    public static (string Subject, string Html, string Text) Confirmation(string name, string confirmationUrl)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeUrl = WebUtility.HtmlEncode(confirmationUrl);
        return (
            "Confirme seu email na AESYN",
            $"""
            <h2>Confirme seu email</h2>
            <p>Ola, {safeName}.</p>
            <p>Confirme seu endereco de email para concluir a configuracao da sua conta AESYN.</p>
            <p><a href=\"{safeUrl}\">Confirmar email</a></p>
            <p>Se voce nao solicitou esta acao, ignore esta mensagem.</p>
            """,
            $"Ola, {name}. Confirme seu email na AESYN: {confirmationUrl}\nSe voce nao solicitou esta acao, ignore esta mensagem.");
    }

    public static (string Subject, string Html, string Text) ChangeEmail(string name, string newEmail, string confirmationUrl)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeEmail = WebUtility.HtmlEncode(newEmail);
        var safeUrl = WebUtility.HtmlEncode(confirmationUrl);
        return (
            "Confirme a troca de email na AESYN",
            $"""
            <h2>Troca de email</h2>
            <p>Ola, {safeName}.</p>
            <p>Foi solicitada a troca do email principal da sua conta para <strong>{safeEmail}</strong>.</p>
            <p><a href=\"{safeUrl}\">Confirmar novo email</a></p>
            <p>Se voce nao solicitou esta alteracao, nao confirme o link e entre em contato com o suporte.</p>
            """,
            $"Ola, {name}. Confirme a troca do email principal para {newEmail}: {confirmationUrl}\nSe voce nao solicitou esta alteracao, nao confirme o link.");
    }
}
