namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class MarketPulseSettings
{
    public bool Enabled { get; set; } = true;

    public bool RunOnStartup { get; set; }

    public string DailyRunTime { get; set; } = "02:30";

    public string SearchKeyword { get; set; } = "cong nghe thong tin";

    public int MaxPagesPerSource { get; set; } = 4;

    public int MaxPostingsPerSource { get; set; } = 160;

    public int ActivePostingLookbackDays { get; set; } = 14;

    public int MissingScansBeforeStale { get; set; } = 3;

    public int MinimumPostingsForLifecycleCheck { get; set; } = 30;

    public int RequestDelaySeconds { get; set; } = 2;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public string PythonExecutablePath { get; set; } = "python";

    public string PythonScriptPath { get; set; } = "Scrapers/topcv_market_pulse.py";
    
    public string[] TrackedKeywords { get; set; } = [];

    public List<MarketPulseSourceSettings> Sources { get; set; } = [];
}

public sealed class MarketPulseSourceSettings
{
    public string Name { get; set; } = null!;

    public string Kind { get; set; } = "Html";

    public string BaseUrl { get; set; } = null!;

    public string SearchUrlTemplate { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    public string[] DetailUrlContains { get; set; } = [];
}
