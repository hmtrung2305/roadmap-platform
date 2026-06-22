using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MarketPulseCrawlRun
{
    public Guid MarketPulseCrawlRunId { get; set; }

    public string SourceName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Mode { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? DurationMs { get; set; }

    public int FetchedCount { get; set; }

    public int SavedCount { get; set; }

    public int DuplicateCount { get; set; }

    public int FailedCount { get; set; }

    public string? StoppedReason { get; set; }

    public string? ErrorSummary { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<MarketPulseFailedItem> MarketPulseFailedItems { get; set; } = new List<MarketPulseFailedItem>();
}
