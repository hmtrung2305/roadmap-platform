namespace RoadmapPlatform.Application.DTOs.Skills;

public sealed class SkillLookupDto
{
    public Guid SkillId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Description { get; set; }

    public IReadOnlyList<string> CareerRoles { get; set; } = [];
}
