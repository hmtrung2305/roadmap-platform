namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class EnrollRoadmapRequestDto
{
    public Guid RoadmapVersionId { get; set; }
}

public sealed class MigrateRoadmapEnrollmentRequestDto
{
    public Guid TargetRoadmapVersionId { get; set; }
}
