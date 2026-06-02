using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IGitHubApiClient
    {
        Task<List<GitHubRepositorySyncDto>> GetPublicRepositoriesAsync(string username);
    }
}
