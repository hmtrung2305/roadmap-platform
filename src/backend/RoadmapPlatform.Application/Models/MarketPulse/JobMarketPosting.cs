namespace RoadmapPlatform.Application.Models.MarketPulse;

public sealed class JobMarketPosting
{
    public string? Id { get; init; }

    public string? SourceJobId { get; init; }

    public string? Source { get; init; }

    public string? Title { get; init; }

    public string? Company { get; init; }

    public string? Category { get; init; }

    public string? Location { get; init; }

    public string? Salary { get; init; }

    public string? SalaryRaw { get; init; }

    public long? SalaryMin { get; init; }

    public long? SalaryMax { get; init; }

    public string? SalaryCurrency { get; init; }

    public bool? SalaryIsNegotiable { get; init; }

    public string? Experience { get; init; }

    public string? ExperienceRaw { get; init; }

    public int? ExperienceMinYears { get; init; }

    public int? ExperienceMaxYears { get; init; }

    public DateOnly? PostedOn { get; init; }

    public string? PostedOnText { get; init; }

    public string? PostDateConfidence { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string? DetailStatus { get; init; }

    public DateTime? DetailLastSuccessAt { get; init; }

    public string? Url { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<string> Requirements { get; init; } = [];

    public IReadOnlyList<string> Specialties { get; init; } = [];

    public IReadOnlyList<string> Benefits { get; init; } = [];

    public IReadOnlyList<string> Skills { get; init; } = [];
}
