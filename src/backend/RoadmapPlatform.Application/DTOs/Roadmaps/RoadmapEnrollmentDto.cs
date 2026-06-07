namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapEnrollmentDto
{
    public Guid RoadmapEnrollmentId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ProgressPercent { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
