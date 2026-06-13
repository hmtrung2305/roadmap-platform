using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleChatService
{
    Task<LearningModuleChatResponseDto> AskAsync(
        Guid userId,
        Guid skillModuleId,
        LearningModuleChatRequestDto request,
        CancellationToken cancellationToken);
}
