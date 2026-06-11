using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IGitHubApiClient
    {
        Task<List<GitHubRepositorySyncDto>> GetPublicRepositoriesAsync(
            string username,
            string accessToken,
            CancellationToken cancellationToken = default);

        Task<string?> GetRepositoryReadmeAsync(
            string owner,
            string repositoryName,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}