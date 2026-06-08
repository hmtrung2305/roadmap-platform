namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseOverviewDto
{
    public DateTime? LastUpdatedAt { get; set; }

    public int TotalPostings { get; set; }

    public int ActivePostings { get; set; }

    public int StalePostings { get; set; }

    public int ExpiredPostings { get; set; }

    public int SourceCount { get; set; }

    public IReadOnlyList<MarketSkillSummaryDto> Skills { get; set; } = [];

    public IReadOnlyList<MarketTrendPointDto> TrendPoints { get; set; } = [];
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
