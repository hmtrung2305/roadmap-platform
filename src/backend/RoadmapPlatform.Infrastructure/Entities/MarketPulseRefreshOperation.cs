namespace RoadmapPlatform.Infrastructure.Entities;

public sealed class MarketPulseRefreshOperation
{
    public Guid MarketPulseRefreshOperationId { get; set; }

    public string Status { get; set; } = "queued";

    public DateTime BaselineCrawlerSuccessAt { get; set; }

    public DateTime? CrawlerSuccessAt { get; set; }

    public Guid? ImportRunId { get; set; }

    public string CurrentStep { get; set; } = "crawler";

    public string TriggerType { get; set; } = "manual";

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
