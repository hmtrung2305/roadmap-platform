namespace RoadmapPlatform.Application.Models.MarketPulse;

public sealed class JobMarketOverviewOptions
{
    public int Days { get; init; } = 14;

    public IReadOnlyCollection<string> SelectedSkillSlugs { get; init; } = [];

    public IReadOnlyCollection<string> TrackedKeywordSpecs { get; init; } = [];

    public DateOnly ReferenceDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int MaxVisibleSkills { get; init; } = 6;

    public int MaxSegmentCount { get; init; } = 8;

    public int MaxTodayJobs { get; init; } = 8;

    public int MaxRecentJobs { get; init; } = 10;
}