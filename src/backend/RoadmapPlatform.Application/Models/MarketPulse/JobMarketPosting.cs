namespace RoadmapPlatform.Application.Models.MarketPulse;

public sealed class JobMarketPosting
{
    public string? Id { get; init; }

    public string? Title { get; init; }

    public string? Company { get; init; }

    public string? Category { get; init; }

    public string? Location { get; init; }

    public string? Salary { get; init; }

    public string? Experience { get; init; }

    public DateOnly? PostedOn { get; init; }

    public string? PostedOnText { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string? Url { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<string> Requirements { get; init; } = [];

    public IReadOnlyList<string> Specialties { get; init; } = [];

    public IReadOnlyList<string> Benefits { get; init; } = [];

    public IReadOnlyList<string> Skills { get; init; } = [];
}
