using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MarketPulseFailedItem
{
    public Guid MarketPulseFailedItemId { get; set; }

    public Guid? MarketPulseCrawlRunId { get; set; }

    public string? Url { get; set; }

    public string Stage { get; set; } = null!;

    public string ErrorCode { get; set; } = null!;

    public string ErrorMessage { get; set; } = null!;

    public string? ErrorDetail { get; set; }

    public string? RawPayload { get; set; }

    public int RetryCount { get; set; }

    public DateTime? LastRetryAt { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual MarketPulseCrawlRun? MarketPulseCrawlRun { get; set; }
}
