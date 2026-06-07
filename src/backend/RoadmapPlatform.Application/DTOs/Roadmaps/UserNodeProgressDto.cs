namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UserNodeProgressDto
{
    public Guid? UserNodeProgressId { get; set; }
    public Guid? RoadmapEnrollmentId { get; set; }
    public Guid RoadmapNodeId { get; set; }
    public string Status { get; set; } = "pending";
    public bool IsComputed { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? LearnerNote { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SkippedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
