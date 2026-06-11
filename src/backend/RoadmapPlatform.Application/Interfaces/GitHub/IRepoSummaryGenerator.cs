using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IRepoSummaryGenerator
    {
        Task<GeneratedRepoInsightDto> GenerateAsync(
            RepoSummaryGenerationRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
