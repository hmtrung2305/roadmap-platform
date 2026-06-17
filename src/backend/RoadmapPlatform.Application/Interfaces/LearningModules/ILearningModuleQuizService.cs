using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleQuizService
{
    Task<LearningModuleQuizDto> UpsertQuizAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        UpsertQuizRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleQuizQuestionDto> AddQuestionAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken);

    Task<LearningModuleQuizQuestionDto> UpdateQuestionAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid questionId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningModuleQuizQuestionDto>> ReorderQuestionsAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        ReorderQuizQuestionsRequestDto request,
        CancellationToken cancellationToken);

    Task DeleteQuestionAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        Guid questionId,
        CancellationToken cancellationToken);
}
