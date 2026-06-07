namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapChildSummaryDto
{
    public Guid RoadmapNodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}
