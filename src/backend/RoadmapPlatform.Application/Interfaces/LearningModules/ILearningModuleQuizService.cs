using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleQuizService
{
    Task<LearningModuleQuizDto> UpsertQuizAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpsertQuizRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleQuizQuestionDto> AddQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleQuizQuestionDto> UpdateQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid questionId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningModuleQuizQuestionDto>> ReorderQuestionsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        ReorderQuizQuestionsRequestDto request,
        CancellationToken cancellationToken);

    Task DeleteQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid questionId,
        CancellationToken cancellationToken);
}
