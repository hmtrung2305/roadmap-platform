using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearnerLearningModuleService
{
    Task<IReadOnlyList<LearnerLearningModuleSummaryDto>> GetPublishedModulesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearnerLearningModuleSummaryDto>> GetEnrolledModulesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<SkillLearningModulesDto> GetPublishedModulesBySkillSlugAsync(
        string skillSlug,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<LearnerLearningModuleOverviewDto> GetPublishedModuleBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<LearningModuleEnrollmentDto> EnrollAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<LearningModuleLessonContentDto> GetLessonContentAsync(
        Guid userId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken);

    Task<UpdateLessonProgressResultDto> UpdateLessonProgressAsync(
        Guid userId,
        Guid skillModuleId,
        Guid lessonId,
        UpdateLessonProgressRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<QuizAttemptSummaryDto>> GetQuizAttemptsAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<StartQuizAttemptResultDto> StartQuizAttemptAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken);

    Task<StartQuizAttemptResultDto> GetQuizAttemptSessionAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        CancellationToken cancellationToken);

    Task<QuizAttemptReviewDto> SubmitQuizAttemptAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        SubmitQuizAttemptRequestDto request,
        CancellationToken cancellationToken);

    Task<QuizAttemptReviewDto> GetQuizAttemptReviewAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        CancellationToken cancellationToken);
}
