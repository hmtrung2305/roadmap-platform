using RoadmapPlatform.Application.DTOs.ContentRoadmaps;

namespace RoadmapPlatform.Application.Interfaces.ContentRoadmaps;

public interface IContentManagerRoadmapService
{
    Task<ContentRoadmapListResultDto> GetRoadmapsAsync(
        ContentRoadmapListQueryDto query,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> GetRoadmapDetailAsync(
        Guid roadmapId,
        Guid? roadmapVersionId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> UpdateRoadmapVersionMetadataAsync(
        Guid roadmapVersionId,
        UpdateRoadmapVersionMetadataRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> UpdateRoadmapNodeMetadataAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeMetadataRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> AddResourceToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeResourceRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> RemoveResourceFromNodeAsync(
        Guid roadmapNodeId,
        Guid learningResourceId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> AddSkillToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeSkillRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> RemoveSkillFromNodeAsync(
        Guid roadmapNodeId,
        Guid skillId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentLearningResourceSearchResultDto>> SearchLearningResourcesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken);
}
