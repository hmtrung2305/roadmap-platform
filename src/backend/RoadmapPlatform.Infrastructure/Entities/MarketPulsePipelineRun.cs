using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

/// <summary>
/// Durable state for every Market Pulse pipeline activity. The operation type
/// separates imports, end-to-end refreshes and the publication-history watermark
/// without requiring three lifecycle tables.
/// </summary>
public partial class MarketPulsePipelineRun
{
    public Guid MarketPulsePipelineRunId { get; set; }

    public string OperationType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Mode { get; set; } = null!;

    public string TriggerType { get; set; } = null!;

    public string CurrentStep { get; set; } = null!;

    public DateTime BaselineCrawlerSuccessAt { get; set; }

    public DateTime? CrawlerSuccessAt { get; set; }

    public Guid? ImportRunId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? DurationMs { get; set; }

    public int FetchedCount { get; set; }

    public int? SourceTotalCount { get; set; }

    public bool IsCompleteSync { get; set; }

    public bool MissingLifecycleApplied { get; set; }

    public string? LifecycleSkippedReason { get; set; }

    public DateTime? SourceGeneratedAt { get; set; }

    public DateTime? SourceLatestSuccessAt { get; set; }

    public int SavedCount { get; set; }

    public int ImportedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int DuplicateCount { get; set; }

    public int FailedCount { get; set; }

    public string? StoppedReason { get; set; }

    public string? ErrorSummary { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? CoverageStart { get; set; }

    public DateTime? CoverageEnd { get; set; }

    public DateTime? SourceDataAt { get; set; }

    public DateTime? LastSuccessfulSyncAt { get; set; }

    public int SyncedPostingCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MarketPulseFailedItem> MarketPulseFailedItems { get; set; } =
        new List<MarketPulseFailedItem>();
}
