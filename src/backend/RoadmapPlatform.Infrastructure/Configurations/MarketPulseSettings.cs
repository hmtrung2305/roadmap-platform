namespace RoadmapPlatform.Infrastructure.Configurations;

/// <summary>
/// Runtime settings for the single supported Market Pulse provider: TopCV Jobs API.
/// TopCV provenance is deliberately not configurable; adding another provider is a new feature.
/// </summary>
public sealed class MarketPulseSettings
{
    public bool Enabled { get; set; }

    public bool RunOnStartup { get; set; }

    public string DailyRunTime { get; set; } = "02:30";

    public string JobsApiUrl { get; set; } = string.Empty;

    // Compatibility alias for existing deployments. It is normalized into JobsApiUrl.
    public string ActiveJobsApiUrl { get; set; } = string.Empty;

    public string JobsApiKey { get; set; } = string.Empty;

    public int JobsApiPageSize { get; set; } = 100;

    public int JobsApiMaxPages { get; set; } = 500;

    public int JobsApiMaxItems { get; set; } = 50_000;

    public string JobsApiOpsHealthUrl { get; set; } = string.Empty;

    public string JobsApiCrawlTriggerUrl { get; set; } = string.Empty;

    public string JobsApiCrawlStatusUrl { get; set; } = string.Empty;

    public int JobsApiHealthTimeoutSeconds { get; set; } = 10;

    public int JobsApiMaxFreshnessHours { get; set; } = 24;

    public bool JobsApiFailOnStaleSource { get; set; } = true;

    public bool JobsApiRequireFreshCrawlMetadata { get; set; } = true;

    public int MissingScansBeforeStale { get; set; } = 3;

    public int MinimumPostingsForLifecycleCheck { get; set; } = 30;

    public bool DisableMissingLifecycleForPartialSync { get; set; } = true;

    public string BusinessTimezone { get; set; } = "Asia/Ho_Chi_Minh";

    public int RetryMax { get; set; } = 3;

    public int BackoffBaseMs { get; set; } = 1_000;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int OverviewCacheSeconds { get; set; } = 120;

    public int HistoryLookbackDays { get; set; } = 400;

    public int RefreshOperationTimeoutMinutes { get; set; } = 30;

    public string InternalApiKey { get; set; } = string.Empty;

    public string[] TrackedKeywords { get; set; } = [];

    public void ApplyEnvironmentAliases()
    {
        Enabled = GetBool("MARKET_PULSE_CRAWL_ENABLED", Enabled);
        JobsApiUrl = GetString(
            "MARKET_PULSE_JOBS_API_URL",
            GetString("MARKET_PULSE_ACTIVE_JOBS_API_URL", FirstNonEmpty(JobsApiUrl, ActiveJobsApiUrl)));
        ActiveJobsApiUrl = JobsApiUrl;
        JobsApiKey = GetString("MARKET_PULSE_JOBS_API_KEY", JobsApiKey);
        JobsApiPageSize = GetInt("MARKET_PULSE_JOBS_API_PAGE_SIZE", JobsApiPageSize);
        JobsApiMaxPages = GetInt("MARKET_PULSE_JOBS_API_MAX_PAGES", JobsApiMaxPages);
        JobsApiMaxItems = GetInt(
            "MARKET_PULSE_JOBS_API_MAX_ITEMS",
            GetInt("MARKET_PULSE_MAX_ITEMS_PER_RUN", JobsApiMaxItems));
        JobsApiOpsHealthUrl = GetString("MARKET_PULSE_JOBS_API_OPS_HEALTH_URL", JobsApiOpsHealthUrl);
        JobsApiCrawlTriggerUrl = GetString("MARKET_PULSE_JOBS_API_CRAWL_TRIGGER_URL", JobsApiCrawlTriggerUrl);
        JobsApiCrawlStatusUrl = GetString("MARKET_PULSE_JOBS_API_CRAWL_STATUS_URL", JobsApiCrawlStatusUrl);
        JobsApiHealthTimeoutSeconds = GetInt(
            "MARKET_PULSE_JOBS_API_HEALTH_TIMEOUT_SECONDS",
            JobsApiHealthTimeoutSeconds);
        JobsApiMaxFreshnessHours = GetInt(
            "MARKET_PULSE_JOBS_API_MAX_FRESHNESS_HOURS",
            JobsApiMaxFreshnessHours);
        JobsApiFailOnStaleSource = GetBool(
            "MARKET_PULSE_JOBS_API_FAIL_ON_STALE_SOURCE",
            JobsApiFailOnStaleSource);
        JobsApiRequireFreshCrawlMetadata = GetBool(
            "MARKET_PULSE_JOBS_API_REQUIRE_FRESH_CRAWL_METADATA",
            JobsApiRequireFreshCrawlMetadata);
        DisableMissingLifecycleForPartialSync = GetBool(
            "MARKET_PULSE_DISABLE_MISSING_LIFECYCLE_FOR_PARTIAL_SYNC",
            DisableMissingLifecycleForPartialSync);
        BusinessTimezone = GetString("MARKET_PULSE_BUSINESS_TIMEZONE", BusinessTimezone);
        HistoryLookbackDays = GetInt("MARKET_PULSE_HISTORY_LOOKBACK_DAYS", HistoryLookbackDays);
    }

    private static bool GetBool(string name, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
              value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase) ||
              value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) ||
              value.Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string GetString(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
