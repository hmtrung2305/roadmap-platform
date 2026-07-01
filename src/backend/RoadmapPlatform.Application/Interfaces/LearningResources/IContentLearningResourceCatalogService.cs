using RoadmapPlatform.Application.DTOs.LearningResources;

namespace RoadmapPlatform.Application.Interfaces.LearningResources;

public interface IContentLearningResourceCatalogService
{
    Task<ContentLearningResourceSearchResultDto> SearchLearningResourcesAsync(
        ContentLearningResourceSearchQueryDto query,
        CancellationToken cancellationToken);

    Task<ContentLearningResourceDto> GetLearningResourceAsync(
        Guid learningResourceId,
        CancellationToken cancellationToken);

    Task<ContentLearningResourceDto> CreateLearningResourceAsync(
        CreateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentLearningResourceDto> UpdateLearningResourceAsync(
        Guid learningResourceId,
        UpdateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken);
}
