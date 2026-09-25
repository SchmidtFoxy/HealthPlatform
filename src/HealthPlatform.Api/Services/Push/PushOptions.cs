namespace HealthPlatform.Api.Services.Push;

public sealed class PushOptions
{
    public const string SectionName = "Push";
    public bool Enabled { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
