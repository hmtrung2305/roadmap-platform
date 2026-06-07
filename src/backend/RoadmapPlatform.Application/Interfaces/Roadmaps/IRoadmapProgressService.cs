using RoadmapPlatform.Application.DTOs.Roadmaps;

namespace RoadmapPlatform.Application.Interfaces.Roadmaps;

public interface IRoadmapProgressService
{
    Task<UpdateNodeProgressResultDto> UpdateNodeProgressAsync(
        Guid userId,
        Guid roadmapEnrollmentId,
        Guid roadmapNodeId,
        UpdateNodeProgressRequestDto request,
        CancellationToken cancellationToken);
}
