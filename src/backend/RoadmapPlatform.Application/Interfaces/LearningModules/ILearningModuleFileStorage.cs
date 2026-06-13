using RoadmapPlatform.Application.DTOs.LearningModules;

namespace RoadmapPlatform.Application.Interfaces.LearningModules;

public interface ILearningModuleFileStorage
{
    string ProviderName { get; }

    Task<StoredLearningModuleFileDto> SaveAsync(
        string objectPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string objectPath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default);
}
