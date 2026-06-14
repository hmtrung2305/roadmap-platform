using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IRepoInsightService
    {
        Task<RepoInsightResponseDto> GenerateInsightAsync(
            Guid userId,
            Guid repositoryId,
            bool force = false,
            CancellationToken cancellationToken = default);
    }
}
