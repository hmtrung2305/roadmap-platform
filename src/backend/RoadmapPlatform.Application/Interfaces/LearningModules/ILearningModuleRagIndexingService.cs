using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleRagIndexingService
{
    Task<IReadOnlyList<LearningModuleChunkDto>> IndexLessonAsync(
        Guid skillModuleId,
        Guid skillModuleLessonId,
        string markdown,
        int expectedContentVersion,
        string? expectedContentHash,
        CancellationToken cancellationToken);

    Task DeleteLessonChunksAsync(
        Guid skillModuleLessonId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningModuleRagSourceDto>> SearchRelevantChunksAsync(
        Guid skillModuleId,
        Guid? preferredLessonId,
        string query,
        int limit,
        CancellationToken cancellationToken);
}
