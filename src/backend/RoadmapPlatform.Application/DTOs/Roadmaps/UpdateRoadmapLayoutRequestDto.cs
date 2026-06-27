namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UpdateRoadmapLayoutRequestDto
{
    public string LayoutDirection { get; set; } = "TB";
    public string? LayoutAlgorithm { get; set; }
}
