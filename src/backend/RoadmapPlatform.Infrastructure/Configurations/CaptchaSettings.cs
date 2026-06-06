namespace RoadmapPlatform.Infrastructure.Configurations;

public class CaptchaSettings
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "Turnstile";

    public string? SiteKey { get; set; }

    public string? SecretKey { get; set; }

    public string VerifyUrl { get; set; } =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}