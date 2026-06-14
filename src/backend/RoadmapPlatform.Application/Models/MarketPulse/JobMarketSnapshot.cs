namespace RoadmapPlatform.Application.Models.MarketPulse;

public sealed class JobMarketSnapshot
{
    public int ActiveTotal { get; init; }

    public int TodayTotal { get; init; }

    public IReadOnlyList<JobMarketPosting> ActiveJobs { get; init; } = [];

    public IReadOnlyList<JobMarketPosting> TodayJobs { get; init; } = [];
}