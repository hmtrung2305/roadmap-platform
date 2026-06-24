using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapService(
    ContentManagerRoadmapQueryService queryService,
    ContentManagerRoadmapMetadataService metadataService,
    ContentManagerRoadmapMappingService mappingService,
    ContentManagerRoadmapStructureService structureService,
    ContentManagerRoadmapDraftService draftService,
    ContentManagerRoadmapValidationService validationService,
    ContentManagerLearningResourceSearchService resourceSearchService) : IContentManagerRoadmapService
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

    public Task<ContentRoadmapDetailDto> CloneRoadmapVersionToDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        return draftService.CloneRoadmapVersionToDraftAsync(roadmapVersionId, request, cancellationToken);
    }

    public Task<ContentRoadmapValidationResultDto> ValidateRoadmapVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        return validationService.ValidateRoadmapVersionAsync(roadmapVersionId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> PublishRoadmapVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        return draftService.PublishRoadmapVersionAsync(roadmapVersionId, cancellationToken);
    }

    public Task DeleteDraftVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        return draftService.DeleteDraftVersionAsync(roadmapVersionId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> CreateNodeAsync(
        Guid roadmapVersionId,
        CreateRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        return structureService.CreateNodeAsync(roadmapVersionId, request, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> MoveNodeAsync(
        Guid roadmapNodeId,
        MoveRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        return structureService.MoveNodeAsync(roadmapNodeId, request, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> DeleteNodeAsync(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        return structureService.DeleteNodeAsync(roadmapNodeId, cancellationToken);
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
