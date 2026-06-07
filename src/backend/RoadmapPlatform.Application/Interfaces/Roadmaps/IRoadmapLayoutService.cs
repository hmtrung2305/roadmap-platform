using RoadmapPlatform.Application.DTOs.Roadmaps;

namespace RoadmapPlatform.Application.Interfaces.Roadmaps;

public interface IRoadmapLayoutService
{
    Task<RoadmapDetailDto> UpdateRoadmapLayoutAsync(
        Guid roadmapVersionId,
        UpdateRoadmapLayoutRequestDto request,
        CancellationToken cancellationToken);
}
