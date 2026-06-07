namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UpdateNodeProgressResultDto
{
    public RoadmapEnrollmentDto Enrollment { get; set; } = new();
    public int TrackableNodeCount { get; set; }
    public int CompletedNodeCount { get; set; }
    public decimal ProgressPercent { get; set; }
    public List<UserNodeProgressDto> ChangedNodes { get; set; } = [];
}
