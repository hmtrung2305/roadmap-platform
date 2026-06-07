namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UpdateNodeProgressRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public string? LearnerNote { get; set; }
    public string? IdempotencyKey { get; set; }
}
