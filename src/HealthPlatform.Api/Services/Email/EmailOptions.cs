namespace HealthPlatform.Api.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@aesyn.com.br";
    public string FromName { get; set; } = "AESYN Performance";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080";
}
