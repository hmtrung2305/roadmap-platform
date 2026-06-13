using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleLessonService
{
    Task<BulkUploadLessonsResultDto> BulkUploadLessonsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        BulkUploadLessonsRequestDto request,
        IReadOnlyList<LearningModuleUploadedFileDto> files,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningModuleLessonDto>> ReorderLessonsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        ReorderLessonsRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonDto> UpdateLessonAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        UpdateLearningModuleLessonRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonDto> ReplaceLessonContentAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        LearningModuleUploadedFileDto file,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonContentDto> GetLessonPreviewAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);

    Task DeleteDraftLessonAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);
}
