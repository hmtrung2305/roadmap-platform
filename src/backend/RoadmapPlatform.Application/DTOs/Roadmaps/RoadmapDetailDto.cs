namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapDetailDto
{
    public Guid RoadmapId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int? EstimatedTotalHours { get; set; }
    public decimal EstimatedRequiredHours { get; set; }
    public decimal EstimatedOptionalHours { get; set; }
    public string LayoutDirection { get; set; } = string.Empty;
    public string? LayoutAlgorithm { get; set; }
    public CareerRoleDto CareerRole { get; set; } = new();
    public RoadmapEnrollmentDto? Enrollment { get; set; }
    public int TrackableNodeCount { get; set; }
    public int CompletedNodeCount { get; set; }
    public decimal ProgressPercent { get; set; }
    public List<RoadmapNodeDto> Nodes { get; set; } = [];
    public List<RoadmapEdgeDto> Edges { get; set; } = [];
}
