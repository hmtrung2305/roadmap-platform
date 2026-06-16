using RoadmapPlatform.Application.Models.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub;

public interface IGitHubTokenService
{
    Task<GitHubAccessTokenContext> GetRequiredAccessTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
