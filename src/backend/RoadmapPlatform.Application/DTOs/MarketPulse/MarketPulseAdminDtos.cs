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
    public int? JobsApiMaxItems { get; set; }

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
    public string Origin { get; set; } = "import";

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

    public int LatestListingJobsSeen { get; set; }

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

    public IReadOnlyList<string> FailureIds { get; set; } = [];

    public IReadOnlyList<MarketPulseFailureActionTargetDto> Failures { get; set; } = [];

    public IReadOnlyCollection<Guid> ResolveImportIds() => FailedItemIds
        .Concat(FailureIds.Select(ParseImportId).Where(id => id.HasValue).Select(id => id!.Value))
        .Concat(Failures
            .Where(target => string.Equals(target.Origin, "import", StringComparison.OrdinalIgnoreCase))
            .Select(target => ParseImportId(target.FailureId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value))
        .Distinct()
        .ToList();

    public IReadOnlyCollection<long> ResolveCrawlerIds() => FailureIds
        .Select(ParseCrawlerId)
        .Concat(Failures
            .Where(target => string.Equals(target.Origin, "crawler", StringComparison.OrdinalIgnoreCase))
            .Select(target => ParseCrawlerId(target.FailureId)))
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct()
        .ToList();

    private static Guid? ParseImportId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return Guid.TryParse(
            value.StartsWith("import:", StringComparison.OrdinalIgnoreCase) ? value[7..] : value,
            out var id)
                ? id
                : null;
    }

    private static long? ParseCrawlerId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return long.TryParse(
            value.StartsWith("crawler:", StringComparison.OrdinalIgnoreCase) ? value[8..] : value,
            out var id)
                ? id
                : null;
    }
}

public sealed class MarketPulseFailureActionTargetDto
{
    public string Origin { get; set; } = string.Empty;

    public string FailureId { get; set; } = string.Empty;
}

public sealed class MarketPulseAdminDashboardDto
{
    public string OverallStatus { get; set; } = "unknown";

    public DateTime? LatestSuccessfulRefreshAt { get; set; }

    public MarketPulseRefreshOperationDto? CurrentOperation { get; set; }

    public int ActiveJobs { get; set; }

    public decimal EstimatedPostings7Days { get; set; }

    public decimal? CrawlerFreshnessHours { get; set; }

    public decimal ReliablePostDateCoverage { get; set; }

    public string AnalyticsConfidence { get; set; } = "low";

    public MarketPulsePostDateQualityDto PostDateQuality { get; set; } = new();

    public decimal? ImportLagMinutes { get; set; }

    public int OpenCrawlerFailures { get; set; }

    public int OpenImportFailures { get; set; }

    public IReadOnlyList<MarketPulsePipelineHealthItemDto> PipelineHealth { get; set; } = [];

    public IReadOnlyList<MarketPulseAdminAlertDto> Alerts { get; set; } = [];

    public IReadOnlyList<MarketPulsePublicationTrendPointDto> DemandTrend { get; set; } = [];

    public IReadOnlyList<MarketPulseRefreshOperationDto> RecentOperations { get; set; } = [];
}

public sealed class MarketPulsePipelineHealthItemDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Status { get; set; } = "unknown";

    public string Detail { get; set; } = string.Empty;
}

public sealed class MarketPulseAdminAlertDto
{
    public string Severity { get; set; } = "info";

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Action { get; set; }
}

public sealed class MarketPulseRefreshOperationDto
{
    public Guid OperationId { get; set; }

    public string Status { get; set; } = "queued";

    public string CurrentStep { get; set; } = "crawler";

    public DateTime BaselineCrawlerSuccessAt { get; set; }

    public DateTime? CrawlerSuccessAt { get; set; }

    public Guid? ImportRunId { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class MarketPulseFailureGroupsDto
{
    public IReadOnlyList<MarketPulseCrawlerFailureDto> CrawlerFailures { get; set; } = [];

    public IReadOnlyList<MarketPulseFailedItemDto> ImportFailures { get; set; } = [];
}

public sealed class MarketPulseCrawlerFailureDto
{
    public string Origin { get; set; } = "crawler";

    public string FailureId { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string Status { get; set; } = "open";

    public bool Actionable { get; set; } = true;

    public int RetryCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastRetryAt { get; set; }
}

public sealed class MarketPulseHistorySyncRequestDto
{
    public int? LookbackDays { get; set; }

    public int? JobsApiPageSize { get; set; }

    public int? JobsApiMaxItems { get; set; }
}
