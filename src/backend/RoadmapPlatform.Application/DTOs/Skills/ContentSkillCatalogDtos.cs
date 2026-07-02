namespace RoadmapPlatform.Application.DTOs.Skills;

public sealed class ContentSkillSearchQueryDto
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public sealed class ContentSkillSearchResultDto
{
    public IReadOnlyList<ContentSkillDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore => Offset + Items.Count < TotalCount;
}

public sealed class ContentSkillDto
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public bool CanEdit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateContentSkillRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class UpdateContentSkillRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}
