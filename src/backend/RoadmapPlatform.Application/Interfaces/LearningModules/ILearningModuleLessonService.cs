using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleLessonService
{

    Task<IReadOnlyList<LearningModuleLessonDto>> GetLessonsAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken);
    Task<BulkUploadLessonsResultDto> BulkUploadLessonsAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        BulkUploadLessonsRequestDto request,
        IReadOnlyList<LearningModuleUploadedFileDto> files,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningModuleLessonDto>> ReorderLessonsAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        ReorderLessonsRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonDto> UpdateLessonAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid lessonId,
        UpdateLearningModuleLessonRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonDto> ReplaceLessonContentAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid lessonId,
        LearningModuleUploadedFileDto file,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonDto> ReindexLessonAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonContentDto> GetLessonPreviewAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);

    Task DeleteDraftLessonAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);
}
