using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IGitHubRepositoryService
    {
        Task<List<GitHubRepositoryResponseDto>> SyncPublicRepositoriesAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<List<GitHubRepositoryResponseDto>> GetSavedRepositoriesAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
