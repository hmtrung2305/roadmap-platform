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

    public IReadOnlyList<MarketSegmentSummaryDto> SourceSummaries { get; set; } = [];

    public IReadOnlyList<MarketJobPostingDto> TodayJobs { get; set; } = [];

    public IReadOnlyList<MarketJobPostingDto> RecentJobs { get; set; } = [];

    public MarketInsightMetaDto InsightMeta { get; set; } = new();

    public MarketDataQualityDto DataQuality { get; set; } = new();

    public IReadOnlyList<MarketPulseInsightDto> InsightCards { get; set; } = [];

    public IReadOnlyList<MarketSkillMovementDto> RisingSkills { get; set; } = [];

    public IReadOnlyList<MarketSkillMovementDto> FallingSkills { get; set; } = [];

    public IReadOnlyList<MarketSkillCoOccurrenceDto> SkillCoOccurrences { get; set; } = [];

    public MarketSalaryInsightDto SalaryInsight { get; set; } = new();

    public IReadOnlyList<MarketSegmentSummaryDto> ExperienceSummaries { get; set; } = [];

    public IReadOnlyList<MarketLearningRecommendationDto> LearningRecommendations { get; set; } = [];
}

public sealed class MarketInsightMetaDto
{
    public int PeriodDays { get; set; }

    public int SampleSize { get; set; }

    public string Confidence { get; set; } = "low";

    public DateTime? LastUpdatedAt { get; set; }

    public string Methodology { get; set; } = string.Empty;
}

public sealed class MarketDataQualityDto
{
    public decimal Score { get; set; }

    public string Level { get; set; } = "low";

    public int SampleSize { get; set; }

    public int SourceCount { get; set; }

    public decimal SalaryCoveragePercent { get; set; }

    public decimal CategoryCoveragePercent { get; set; }

    public decimal LocationCoveragePercent { get; set; }

    public decimal DetailCoveragePercent { get; set; }

    public decimal OtherCategoryPercent { get; set; }

    public int FreshnessHours { get; set; }

    public IReadOnlyList<string> Warnings { get; set; } = [];
}

public sealed class MarketSkillSummaryDto
{
    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public int MentionCount { get; set; }

    public int PostingCount { get; set; }

    public decimal GrowthPercent { get; set; }
}

public sealed class MarketPulseInsightDto
{
    public string Title { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Detail { get; set; } = null!;

    public string Tone { get; set; } = "neutral";

    public int SampleSize { get; set; }

    public int PeriodDays { get; set; }

    public string Confidence { get; set; } = "low";

    public DateTime? LastUpdatedAt { get; set; }
}

public sealed class MarketSkillMovementDto
{
    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public int CurrentMentions { get; set; }

    public int PreviousMentions { get; set; }

    public int Delta { get; set; }

    public decimal GrowthPercent { get; set; }

    public int SampleSize { get; set; }

    public int PeriodDays { get; set; }

    public string Confidence { get; set; } = "low";
}

public sealed class MarketSkillCoOccurrenceDto
{
    public string SkillA { get; set; } = null!;

    public string SkillASlug { get; set; } = null!;

    public string SkillB { get; set; } = null!;

    public string SkillBSlug { get; set; } = null!;

    public int PostingCount { get; set; }

    public decimal PercentOfSample { get; set; }

    public int SampleSize { get; set; }

    public string Confidence { get; set; } = "low";
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

public sealed class MarketSalaryInsightDto
{
    public int SampleSize { get; set; }

    public decimal CoveragePercent { get; set; }

    public decimal? MedianMinMonthlyVnd { get; set; }

    public decimal? MedianMaxMonthlyVnd { get; set; }

    public decimal? LowestMonthlyVnd { get; set; }

    public decimal? HighestMonthlyVnd { get; set; }

    public string Confidence { get; set; } = "low";

    public IReadOnlyList<MarketSalarySegmentDto> ByCategory { get; set; } = [];
}

public sealed class MarketSalarySegmentDto
{
    public string Name { get; set; } = null!;

    public int SampleSize { get; set; }

    public decimal CoveragePercent { get; set; }

    public decimal? MedianMinMonthlyVnd { get; set; }

    public decimal? MedianMaxMonthlyVnd { get; set; }
}

public sealed class MarketLearningRecommendationDto
{
    public string Title { get; set; } = null!;

    public string Detail { get; set; } = null!;

    public string ActionLabel { get; set; } = null!;

    public string? SkillSlug { get; set; }

    public string Priority { get; set; } = "medium";

    public int SampleSize { get; set; }

    public string Confidence { get; set; } = "low";
}

public sealed class MarketJobPostingDto
{
    public string Id { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Company { get; set; }

    public string? Source { get; set; }

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
