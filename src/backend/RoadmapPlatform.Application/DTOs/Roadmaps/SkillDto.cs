namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class SkillDto
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Category { get; set; }
}
