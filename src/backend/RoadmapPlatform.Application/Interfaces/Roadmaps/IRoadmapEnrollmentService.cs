using RoadmapPlatform.Application.DTOs.Roadmaps;

namespace RoadmapPlatform.Application.Interfaces.Roadmaps;

public interface IRoadmapEnrollmentService
{
    Task<RoadmapEnrollmentDto> EnrollAsync(
        Guid userId,
        EnrollRoadmapRequestDto request,
        CancellationToken cancellationToken);

    Task<RoadmapEnrollmentDto?> GetCurrentEnrollmentAsync(
        Guid userId,
        Guid roadmapVersionId,
        CancellationToken cancellationToken);

    Task<RoadmapEnrollmentDto> MigrateEnrollmentAsync(
        Guid userId,
        Guid roadmapEnrollmentId,
        MigrateRoadmapEnrollmentRequestDto request,
        CancellationToken cancellationToken);
}
