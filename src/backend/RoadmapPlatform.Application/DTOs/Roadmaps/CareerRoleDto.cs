namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class CareerRoleDto
{
    public Guid CareerRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
}
