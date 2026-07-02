using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapService(
    ApplicationDbContext dbContext,
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
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken)
    {
        return queryService.GetRoadmapsAsync(query, actorUserId, includeAllRoadmaps, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> GetRoadmapDetailAsync(
        Guid roadmapId,
        Guid? roadmapVersionId,
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken)
    {
        return queryService.GetRoadmapDetailAsync(roadmapId, roadmapVersionId, actorUserId, includeAllRoadmaps, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> CreateRoadmapAsync(
        CreateRoadmapRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.CreateRoadmapAsync(request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> CloneRoadmapVersionToDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.CloneRoadmapVersionToDraftAsync(roadmapVersionId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> CreatePatchRoadmapVersionDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.CreatePatchRoadmapVersionDraftAsync(roadmapVersionId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> CreateMinorRoadmapVersionDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.CreateMinorRoadmapVersionDraftAsync(roadmapVersionId, request, actorUserId, cancellationToken);
    }

    public async Task<ContentRoadmapValidationResultDto> ValidateRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken)
    {
        if (!includeAllRoadmaps)
        {
            await ContentManagerRoadmapOwnership.EnsureVersionOwnedByActorAsync(
                dbContext,
                roadmapVersionId,
                actorUserId,
                cancellationToken);
        }

        return await validationService.ValidateRoadmapVersionAsync(roadmapVersionId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> PublishRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.PublishRoadmapVersionAsync(roadmapVersionId, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> SubmitRoadmapVersionForReviewAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        SubmitRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        return draftService.SubmitRoadmapVersionForReviewAsync(roadmapVersionId, actorUserId, request, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> ApproveRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.ApproveRoadmapVersionAsync(roadmapVersionId, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> RejectRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        RejectRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        return draftService.RejectRoadmapVersionAsync(roadmapVersionId, actorUserId, request, cancellationToken);
    }

    public Task DeleteDraftVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return draftService.DeleteDraftVersionAsync(roadmapVersionId, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> CreateNodeAsync(
        Guid roadmapVersionId,
        CreateRoadmapNodeRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return structureService.CreateNodeAsync(roadmapVersionId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> MoveNodeAsync(
        Guid roadmapNodeId,
        MoveRoadmapNodeRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return structureService.MoveNodeAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> UpdateGroupRuleAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeGroupRuleRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return structureService.UpdateGroupRuleAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> UpdateNodeRequirementAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeRequirementRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return structureService.UpdateNodeRequirementAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapStructureMutationResultDto> DeleteNodeAsync(
        Guid roadmapNodeId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return structureService.DeleteNodeAsync(roadmapNodeId, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapDetailDto> UpdateRoadmapVersionMetadataAsync(
        Guid roadmapVersionId,
        UpdateRoadmapVersionMetadataRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return metadataService.UpdateRoadmapVersionMetadataAsync(roadmapVersionId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> UpdateRoadmapNodeMetadataAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeMetadataRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return metadataService.UpdateRoadmapNodeMetadataAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> AddResourceToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeResourceRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return mappingService.AddResourceToNodeAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> RemoveResourceFromNodeAsync(
        Guid roadmapNodeId,
        Guid learningResourceId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return mappingService.RemoveResourceFromNodeAsync(roadmapNodeId, learningResourceId, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> AddSkillToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeSkillRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return mappingService.AddSkillToNodeAsync(roadmapNodeId, request, actorUserId, cancellationToken);
    }

    public Task<ContentRoadmapNodeDto> RemoveSkillFromNodeAsync(
        Guid roadmapNodeId,
        Guid skillId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return mappingService.RemoveSkillFromNodeAsync(roadmapNodeId, skillId, actorUserId, cancellationToken);
    }

    public Task<IReadOnlyList<ContentLearningResourceSearchResultDto>> SearchLearningResourcesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        return resourceSearchService.SearchLearningResourcesAsync(search, limit, cancellationToken);
    }
}
