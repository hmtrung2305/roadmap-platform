using RoadmapPlatform.Application.DTOs.Roadmaps;

namespace RoadmapPlatform.Application.Interfaces.Roadmaps;

public interface IRoadmapQueryService
{
    Task<IReadOnlyList<RoadmapSummaryDto>> GetPublishedRoadmapsAsync(CancellationToken cancellationToken);

    Task<RoadmapDetailDto> GetPublishedRoadmapBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<RoadmapDetailDto> GetRoadmapDetailByVersionIdAsync(
        Guid roadmapVersionId,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<RoadmapGraphDto> GetPublishedRoadmapGraphBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<RoadmapNodeDetailDto> GetRoadmapNodeDetailAsync(
        Guid roadmapVersionId,
        Guid roadmapNodeId,
        Guid? userId,
        CancellationToken cancellationToken);
}
