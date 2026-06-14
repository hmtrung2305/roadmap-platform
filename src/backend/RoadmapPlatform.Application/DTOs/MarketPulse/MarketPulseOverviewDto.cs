namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseOverviewDto
{
    public DateTime? LastUpdatedAt { get; set; }

    public int TotalPostings { get; set; }

    public int ActivePostings { get; set; }

    public int TodayPostings { get; set; }

    public int StalePostings { get; set; }

    public int ExpiredPostings { get; set; }

    public int SourceCount { get; set; }

    public IReadOnlyList<MarketSkillSummaryDto> Skills { get; set; } = [];

    public IReadOnlyList<MarketSkillSummaryDto> TodaySkills { get; set; } = [];

    public IReadOnlyList<MarketTrendPointDto> TrendPoints { get; set; } = [];

    public IReadOnlyList<MarketSegmentSummaryDto> CategorySummaries { get; set; } = [];

    public IReadOnlyList<MarketSegmentSummaryDto> LocationSummaries { get; set; } = [];

    public IReadOnlyList<MarketJobPostingDto> TodayJobs { get; set; } = [];

    public IReadOnlyList<MarketJobPostingDto> RecentJobs { get; set; } = [];
}

public sealed class MarketSkillSummaryDto
{
    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public int MentionCount { get; set; }

    public int PostingCount { get; set; }

    public decimal GrowthPercent { get; set; }
}

public sealed class MarketTrendPointDto
{
    public DateTime Date { get; set; }

    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public int MentionCount { get; set; }

    public int PostingCount { get; set; }
}

public sealed class MarketSegmentSummaryDto
{
    public string Name { get; set; } = null!;

    public int Count { get; set; }

    public decimal Percent { get; set; }
}

public sealed class MarketJobPostingDto
{
    public string Id { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Company { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Salary { get; set; }

    public string? Experience { get; set; }

    public DateTime? PostDate { get; set; }

    public string? PostDateText { get; set; }

    public string Url { get; set; } = null!;

    public bool IsActive { get; set; }

    public IReadOnlyList<string> Requirements { get; set; } = [];

    public IReadOnlyList<string> Specialties { get; set; } = [];
}