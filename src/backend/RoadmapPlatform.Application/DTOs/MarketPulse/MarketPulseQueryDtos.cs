namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseOverviewQueryDto
{
    public int Days { get; set; } = 30;

    public IReadOnlyCollection<string> SkillSlugs { get; set; } = [];

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Experience { get; set; }

    public string? Source { get; set; }

    public decimal? SalaryMinMonthlyVnd { get; set; }

    public decimal? SalaryMaxMonthlyVnd { get; set; }
}

public sealed class MarketPulseIngestRequestDto
{
    public string SourceName { get; set; } = "topcv";

    public IReadOnlyList<MarketPulseIngestPostingDto> Postings { get; set; } = [];
}

public sealed class MarketPulseIngestPostingDto
{
    public string? Id { get; set; }

    public string? SourceJobId { get; set; }

    public string? Title { get; set; }

    public string? Company { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Salary { get; set; }

    public string? Experience { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? PostDateText { get; set; }

    public string? PostDateConfidence { get; set; }

    public DateTime? PostDateLowerBound { get; set; }

    public DateTime? PostDateUpperBound { get; set; }

    public DateTime? PostDateObservedOn { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? Url { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<string> Requirements { get; set; } = [];

    public IReadOnlyList<string> Specialties { get; set; } = [];

    public IReadOnlyList<string> Benefits { get; set; } = [];

    public IReadOnlyList<string> Skills { get; set; } = [];
}
