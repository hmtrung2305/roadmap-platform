namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapGraphNodeDto
{
    public Guid RoadmapNodeId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? CheckpointType { get; set; }
    public string? SelectionType { get; set; }
    public int? RequiredCount { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string LayoutRole { get; set; } = string.Empty;
    public decimal EstimatedRequiredHours { get; set; }
    public decimal EstimatedOptionalHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsTrackable { get; set; }
    public UserNodeProgressDto Progress { get; set; } = new();
}
