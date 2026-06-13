using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ICounselorLearningModuleService
{
    Task<IReadOnlyList<CounselorLearningModuleSummaryDto>> GetModulesAsync(
        Guid counselorUserId,
        string? status,
        CancellationToken cancellationToken);

    Task<CounselorLearningModuleDetailDto> GetModuleDetailAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> CreateModuleAsync(
        Guid counselorUserId,
        CreateLearningModuleRequestDto request,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> UpdateModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken);

    Task DeleteDraftModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<PublishLearningModuleReadinessDto> GetPublishReadinessAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<PublishLearningModuleResultDto> PublishModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> ArchiveModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<SkillModuleDto> RestoreModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<LearningModulePreviewDto> GetPreviewAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);
}
