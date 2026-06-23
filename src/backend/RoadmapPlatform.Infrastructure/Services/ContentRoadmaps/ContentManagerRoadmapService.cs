using RoadmapPlatform.Application.DTOs.ContentRoadmaps;
using RoadmapPlatform.Application.Interfaces.ContentRoadmaps;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

public sealed class ContentManagerRoadmapService(
    ContentRoadmapQueryService queryService,
    ContentRoadmapMetadataService metadataService,
    ContentRoadmapMappingService mappingService,
    ContentLearningResourceSearchService resourceSearchService) : IContentManagerRoadmapService
{
    public Task<ContentRoadmapListResultDto> GetRoadmapsAsync(
        ContentRoadmapListQueryDto query,
        CancellationToken cancellationToken)
    {
        return queryService.GetRoadmapsAsync(query, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> GetRoadmapDetailAsync(
        Guid roadmapId,
        Guid? roadmapVersionId,
        CancellationToken cancellationToken)
    {
        return queryService.GetRoadmapDetailAsync(roadmapId, roadmapVersionId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> UpdateRoadmapVersionMetadataAsync(
        Guid roadmapVersionId,
        UpdateRoadmapVersionMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        return metadataService.UpdateRoadmapVersionMetadataAsync(roadmapVersionId, request, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> UpdateRoadmapNodeMetadataAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        return metadataService.UpdateRoadmapNodeMetadataAsync(roadmapNodeId, request, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> AddResourceToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        return mappingService.AddResourceToNodeAsync(roadmapNodeId, request, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> RemoveResourceFromNodeAsync(
        Guid roadmapNodeId,
        Guid learningResourceId,
        CancellationToken cancellationToken)
    {
        return mappingService.RemoveResourceFromNodeAsync(roadmapNodeId, learningResourceId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> AddSkillToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        return mappingService.AddSkillToNodeAsync(roadmapNodeId, request, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> RemoveSkillFromNodeAsync(
        Guid roadmapNodeId,
        Guid skillId,
        CancellationToken cancellationToken)
    {
        return mappingService.RemoveSkillFromNodeAsync(roadmapNodeId, skillId, cancellationToken);
    }

    public Task<IReadOnlyList<ContentLearningResourceSearchResultDto>> SearchLearningResourcesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        return resourceSearchService.SearchLearningResourcesAsync(search, limit, cancellationToken);
    }
}
