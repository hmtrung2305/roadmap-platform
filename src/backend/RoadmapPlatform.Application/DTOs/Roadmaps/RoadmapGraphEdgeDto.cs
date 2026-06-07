namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapGraphEdgeDto
{
    public Guid RoadmapEdgeId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string EdgeType { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
}
