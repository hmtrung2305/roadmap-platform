using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleAssistantService
{
    Task<ModuleAssistantResponseDto> AskAsync(
        Guid userId,
        Guid skillModuleId,
        ModuleAssistantRequestDto request,
        CancellationToken cancellationToken);
}
