namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class MarketPulseSettings
{
    public bool Enabled { get; set; }

    public bool RunOnStartup { get; set; }

    public string DailyRunTime { get; set; } = "02:30";

    public string SearchKeyword { get; set; } = "cong nghe thong tin";

    public int MaxPagesPerSource { get; set; } = 4;

    public int MaxPostingsPerSource { get; set; } = 160;

    public int MaxItemsPerRun { get; set; } = 500;

    public int JobsApiPageSize { get; set; } = 100;

    public int JobsApiMaxPages { get; set; } = 10;

    public int ActivePostingLookbackDays { get; set; } = 14;

    public int MissingScansBeforeStale { get; set; } = 3;

    public int MinimumPostingsForLifecycleCheck { get; set; } = 30;

    public int RequestDelaySeconds { get; set; } = 2;

    public int DelayMinMs { get; set; } = 1_500;

    public int DelayMaxMs { get; set; } = 4_500;

    public int RetryMax { get; set; } = 3;

    public int BackoffBaseMs { get; set; } = 1_000;

    public int SourceRateLimit { get; set; } = 1;

    public string ScheduleCron { get; set; } = "30 2 * * *";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int OverviewCacheSeconds { get; set; } = 120;

    public string InternalApiKey { get; set; } = string.Empty;

    public string ActiveJobsApiUrl { get; set; } = string.Empty;

    public string TodayJobsApiUrl { get; set; } = string.Empty;
    
    public string[] TrackedKeywords { get; set; } = [];

    public List<MarketPulseSourceSettings> Sources { get; set; } = [];

    public void ApplyEnvironmentAliases()
    {
        Enabled = GetBool("MARKET_PULSE_CRAWL_ENABLED", Enabled);
        MaxPagesPerSource = GetInt("MARKET_PULSE_MAX_PAGES_PER_RUN", MaxPagesPerSource);
        MaxPostingsPerSource = GetInt("MARKET_PULSE_MAX_ITEMS_PER_RUN", MaxPostingsPerSource);
        MaxItemsPerRun = GetInt("MARKET_PULSE_MAX_ITEMS_PER_RUN", MaxItemsPerRun);
        DelayMinMs = GetInt("MARKET_PULSE_DELAY_MIN_MS", DelayMinMs);
        DelayMaxMs = GetInt("MARKET_PULSE_DELAY_MAX_MS", DelayMaxMs);
        RetryMax = GetInt("MARKET_PULSE_RETRY_MAX", RetryMax);
        BackoffBaseMs = GetInt("MARKET_PULSE_BACKOFF_BASE_MS", BackoffBaseMs);
        SourceRateLimit = GetInt("MARKET_PULSE_SOURCE_RATE_LIMIT", SourceRateLimit);
        ScheduleCron = GetString("MARKET_PULSE_SCHEDULE_CRON", ScheduleCron);
        ActiveJobsApiUrl = GetString("MARKET_PULSE_ACTIVE_JOBS_API_URL", ActiveJobsApiUrl);
        TodayJobsApiUrl = GetString("MARKET_PULSE_TODAY_JOBS_API_URL", TodayJobsApiUrl);
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
}

public sealed class MarketPulseSourceSettings
{
    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = "Html";

    public string BaseUrl { get; set; } = string.Empty;

    public string SearchUrlTemplate { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string[] DetailUrlContains { get; set; } = [];
}
