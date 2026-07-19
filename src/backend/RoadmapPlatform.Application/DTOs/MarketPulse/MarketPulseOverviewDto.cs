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

    public MarketPulsePublicationAnalyticsDto PublicationAnalytics { get; set; } = new();

    // Compatibility field for one release. Crawl-observation analytics has been retired.
    public MarketPulseObservationAnalyticsDto? ObservationAnalytics { get; set; }
}

public sealed class MarketPulsePublicationAnalyticsDto
{
    public string Basis { get; set; } = "published_date";

    public string DateModel { get; set; } = "interval_weighted";

    public string Availability { get; set; } = "history_sync_required";

    public DateTime AnchorDate { get; set; }

    public DateTime? SourceDataAt { get; set; }

    public DateTime? HistoryCoverageStart { get; set; }

    public DateTime? HistoryCoverageEnd { get; set; }

    public string Confidence { get; set; } = "low";

    public MarketPulsePublicationPeriodDto CurrentPeriod { get; set; } = new();

    public MarketPulsePublicationPeriodDto PreviousPeriod { get; set; } = new();

    public IReadOnlyList<MarketPulsePublicationTrendPointDto> MarketTrendPoints { get; set; } = [];

    public MarketPulsePublicationComparisonDto MarketComparison { get; set; } = new();

    public IReadOnlyList<MarketPulsePublicationSkillTrendPointDto> SkillTrendPoints { get; set; } = [];

    public IReadOnlyList<MarketPulsePublicationSkillComparisonDto> SkillComparisons { get; set; } = [];

    public MarketPulsePostDateQualityDto PostDateQuality { get; set; } = new();
}

public sealed class MarketPulsePublicationPeriodDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int ExpectedDays { get; set; }

    public int CoveredDays { get; set; }

    public decimal EstimatedTotal { get; set; }

    public int ExactCount { get; set; }

    public decimal RelativeEstimate { get; set; }

    public decimal? AveragePerDay { get; set; }
}

public sealed class MarketPulsePublicationTrendPointDto
{
    public DateTime Date { get; set; }

    public bool Available { get; set; }

    public decimal? ExactPostings { get; set; }

    public decimal? RelativeEstimate { get; set; }

    public decimal? TotalEstimate { get; set; }
}

public sealed class MarketPulsePublicationComparisonDto
{
    public decimal? CurrentTotal { get; set; }

    public decimal? PreviousTotal { get; set; }

    public decimal? CurrentAverage { get; set; }

    public decimal? PreviousAverage { get; set; }

    public decimal? Delta { get; set; }

    public decimal? GrowthPercent { get; set; }

    public string Direction { get; set; } = "insufficient";

    public string Confidence { get; set; } = "low";
}

public sealed class MarketPulsePublicationSkillTrendPointDto
{
    public DateTime Date { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string SkillSlug { get; set; } = string.Empty;

    public bool Available { get; set; }

    public decimal? ExactPostings { get; set; }

    public decimal? RelativeEstimate { get; set; }

    public decimal? TotalEstimate { get; set; }
}

public sealed class MarketPulsePublicationSkillComparisonDto
{
    public string SkillName { get; set; } = string.Empty;

    public string SkillSlug { get; set; } = string.Empty;

    public decimal? CurrentTotal { get; set; }

    public decimal? PreviousTotal { get; set; }

    public decimal? CurrentAverage { get; set; }

    public decimal? PreviousAverage { get; set; }

    public decimal? Delta { get; set; }

    public decimal? GrowthPercent { get; set; }

    public string Direction { get; set; } = "insufficient";

    public string Confidence { get; set; } = "low";
}

public sealed class MarketPulseObservationAnalyticsDto
{
    public string Basis { get; set; } = "crawl_observation";

    public string Availability { get; set; } = "no_observations";

    public MarketPulseObservationPeriodDto CurrentPeriod { get; set; } = new();

    public MarketPulseObservationPeriodDto PreviousPeriod { get; set; } = new();

    public DateTime? LatestObservedAt { get; set; }

    public string Confidence { get; set; } = "low";

    public IReadOnlyList<MarketPulseMarketTrendPointDto> MarketTrendPoints { get; set; } = [];

    public MarketPulseMarketComparisonDto MarketComparison { get; set; } = new();

    public IReadOnlyList<MarketPulseObservedSkillTrendPointDto> SkillTrendPoints { get; set; } = [];

    public IReadOnlyList<MarketPulseSkillPeriodComparisonDto> SkillComparisons { get; set; } = [];

    public MarketPulsePostDateQualityDto PostDateQuality { get; set; } = new();
}

public sealed class MarketPulseObservationPeriodDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int ExpectedDays { get; set; }

    public int ObservedDays { get; set; }

    public decimal CoveragePercent { get; set; }
}

public sealed class MarketPulseMarketTrendPointDto
{
    public DateTime Date { get; set; }

    public int? ObservedPostings { get; set; }

    public int? ActivePostings { get; set; }

    public int? NewPostings { get; set; }

    public int? SourceCount { get; set; }

    public bool HasObservation { get; set; }
}

public sealed class MarketPulseMarketComparisonDto
{
    public MarketPulsePeriodComparisonDto ObservedPostings { get; set; } = new();

    public MarketPulsePeriodComparisonDto ActivePostings { get; set; } = new();

    public MarketPulsePeriodComparisonDto NewPostingsPerObservedDay { get; set; } = new();
}

public sealed class MarketPulsePeriodComparisonDto
{
    public decimal? CurrentAverage { get; set; }

    public decimal? PreviousAverage { get; set; }

    public decimal? Delta { get; set; }

    public decimal? GrowthPercent { get; set; }

    public string Direction { get; set; } = "insufficient";

    public string Confidence { get; set; } = "low";
}

public sealed class MarketPulseObservedSkillTrendPointDto
{
    public DateTime Date { get; set; }

    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public int? PostingCount { get; set; }

    public bool HasObservation { get; set; }
}

public sealed class MarketPulseSkillPeriodComparisonDto
{
    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public decimal? CurrentAverage { get; set; }

    public decimal? PreviousAverage { get; set; }

    public decimal? Delta { get; set; }

    public decimal? GrowthPercent { get; set; }

    public string Direction { get; set; } = "insufficient";

    public string Confidence { get; set; } = "low";
}

public sealed class MarketPulsePostDateQualityDto
{
    public int SampleSize { get; set; }

    public int ExactCount { get; set; }

    public int RelativeCount { get; set; }

    public int UnknownCount { get; set; }

    public decimal ExactPercent { get; set; }

    public decimal RelativePercent { get; set; }

    public decimal UnknownPercent { get; set; }

    public decimal ReliablePercent { get; set; }

    public decimal AverageIntervalWidthDays { get; set; }

    public decimal BroadRangeSharePercent { get; set; }

    public string Confidence { get; set; } = "low";
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
