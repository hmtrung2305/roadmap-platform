namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseAdminQueryDto
{
    public string? Status { get; set; }

    public string? Source { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int Limit { get; set; } = 50;
}

public sealed class MarketPulseRefreshRequestDto
{
    public int? MaxPagesPerSource { get; set; }

    public int? MaxPostingsPerSource { get; set; }

    public int? JobsApiPageSize { get; set; }

    public int? JobsApiMaxPages { get; set; }
}

public sealed class MarketPulseCrawlRunDto
{
    public Guid RunId { get; set; }

    public string Source { get; set; } = "all";

    public string Status { get; set; } = "running";

    public string Mode { get; set; } = "manual";

    public string TriggerType { get; set; } = "manual";

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
}

public sealed class MarketPulseFailedItemDto
{
    public Guid FailedItemId { get; set; }

    public Guid? RunId { get; set; }

    public string Source { get; set; } = "unknown";

    public string? Url { get; set; }

    public string Stage { get; set; } = "unknown";

    public string ErrorCode { get; set; } = "UNKNOWN";

    public string ErrorMessage { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastRetryAt { get; set; }

    public string Status { get; set; } = "open";

    public string? ErrorDetail { get; set; }
}

public sealed class MarketPulseClassifierMappingDto
{
    public Guid MappingId { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public string Category { get; set; } = "Other";

    public bool IsEnabled { get; set; } = true;

    public decimal Weight { get; set; } = 1m;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class MarketPulseClassifierMappingRequestDto
{
    public string Keyword { get; set; } = string.Empty;

    public string Category { get; set; } = "Other";

    public bool IsEnabled { get; set; } = true;

    public decimal Weight { get; set; } = 1m;
}

public sealed class MarketPulseClassifierTestRequestDto
{
    public string Text { get; set; } = string.Empty;
}

public sealed class MarketPulseClassifierTestResultDto
{
    public string Category { get; set; } = "Other";

    public decimal Confidence { get; set; }

    public string FallbackCategory { get; set; } = "Other";

    public IReadOnlyList<MarketPulseClassifierMatchDto> Matches { get; set; } = [];
}

public sealed class MarketPulseClassifierMatchDto
{
    public string Keyword { get; set; } = string.Empty;

    public string Category { get; set; } = "Other";

    public decimal Weight { get; set; }
}

public sealed class MarketPulseSourceHealthDto
{
    public string Source { get; set; } = "unknown";

    public string Status { get; set; } = "unknown";

    public DateTime? LastSuccessAt { get; set; }

    public DateTime? LastFailureAt { get; set; }

    public DateTime? SourceGeneratedAt { get; set; }

    public DateTime? SourceLatestSuccessAt { get; set; }

    public int ConsecutiveFailures { get; set; }

    public Guid? LastRunId { get; set; }

    public string? LastErrorSummary { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class MarketPulseExternalSourceHealthDto
{
    public bool IsAvailable { get; set; }

    public string Status { get; set; } = "unavailable";

    public bool IsStale { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CheckedAt { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public DateTime? LatestSuccessfulCrawlAt { get; set; }

    public double? HoursSinceSuccessfulCrawl { get; set; }

    public string? LatestListingStatus { get; set; }

    public DateTime? LatestListingStartedAt { get; set; }

    public DateTime? LatestListingFinishedAt { get; set; }

    public int PagesBlocked { get; set; }

    public int PagesFailed { get; set; }

    public int ActiveJobs { get; set; }

    public int NewJobsToday { get; set; }

    public decimal DetailCompletionRate { get; set; }

    public IReadOnlyList<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public sealed class MarketPulseBulkActionRequestDto
{
    public IReadOnlyList<Guid> FailedItemIds { get; set; } = [];
}
