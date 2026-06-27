namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UpdateNodeProgressRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}
