using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

public interface IContentManagerRoadmapService
{
    Task<ContentRoadmapListResultDto> GetRoadmapsAsync(
        ContentRoadmapListQueryDto query,
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> GetRoadmapDetailAsync(
        Guid roadmapId,
        Guid? roadmapVersionId,
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> CreateRoadmapAsync(
        CreateRoadmapRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> UpdateRoadmapVersionMetadataAsync(
        Guid roadmapVersionId,
        UpdateRoadmapVersionMetadataRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> CloneRoadmapVersionToDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> CreatePatchRoadmapVersionDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> CreateMinorRoadmapVersionDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapValidationResultDto> ValidateRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        bool includeAllRoadmaps,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> SubmitRoadmapVersionForReviewAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        SubmitRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> ApproveRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> RejectRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        RejectRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentRoadmapDetailDto> PublishRoadmapVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task DeleteDraftVersionAsync(
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapStructureMutationResultDto> CreateNodeAsync(
        Guid roadmapVersionId,
        CreateRoadmapNodeRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapStructureMutationResultDto> MoveNodeAsync(
        Guid roadmapNodeId,
        MoveRoadmapNodeRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapStructureMutationResultDto> UpdateGroupRuleAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeGroupRuleRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapStructureMutationResultDto> UpdateNodeRequirementAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeRequirementRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapStructureMutationResultDto> DeleteNodeAsync(
        Guid roadmapNodeId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> UpdateRoadmapNodeMetadataAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeMetadataRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> AddResourceToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeResourceRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> RemoveResourceFromNodeAsync(
        Guid roadmapNodeId,
        Guid learningResourceId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> AddSkillToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeSkillRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ContentRoadmapNodeDto> RemoveSkillFromNodeAsync(
        Guid roadmapNodeId,
        Guid skillId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentLearningResourceSearchResultDto>> SearchLearningResourcesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken);
}
