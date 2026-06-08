namespace RoadmapPlatform.Infrastructure.Configurations;

public class BrevoEmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Roadmap Platform";
}
