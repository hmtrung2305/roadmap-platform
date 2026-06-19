using RoadmapPlatform.Application.DTOs.ContentWorkspace;
using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface IContentManagerLearningModuleService
{
    Task<ContentWorkspaceOverviewDto> GetWorkspaceOverviewAsync(
        Guid contentManagerUserId,
        CancellationToken cancellationToken);

    Task<ContentManagerLearningModuleListResultDto> GetModulesAsync(
        Guid contentManagerUserId,
        ContentManagerLearningModuleListQueryDto query,
        CancellationToken cancellationToken);

    Task<ContentManagerLearningModuleDetailDto> GetModuleDetailAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> GetModuleOverviewAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> CreateModuleAsync(
        Guid contentManagerUserId,
        CreateLearningModuleRequestDto request,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> UpdateModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken);

    Task DeleteDraftModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<PublishLearningModuleReadinessDto> GetPublishReadinessAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<PublishLearningModuleResultDto> PublishModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> ArchiveModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<LearningModulePreviewDto> GetPreviewAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);
}
