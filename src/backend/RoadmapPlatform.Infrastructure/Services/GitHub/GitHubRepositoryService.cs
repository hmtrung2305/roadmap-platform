using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.GitHub
{
    public class GitHubRepositoryService : IGitHubRepositoryService
    {
        private readonly IGitHubApiClient _gitHubApiClient;
        private readonly ApplicationDbContext _dbContext;

        public GitHubRepositoryService(IGitHubApiClient gitHubApiClient, ApplicationDbContext dbContext)
        {
            _gitHubApiClient = gitHubApiClient;
            _dbContext = dbContext;
        }

        public async Task<List<GitHubRepositoryResponseDto>> SyncPublicRepositoriesAsync(Guid userId)
        {
            var githubProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                x.UserId == userId && x.Provider == AuthProviders.GitHub);

            if (githubProvider == null)
            {
                throw new InvalidOperationException("GitHub account is not linked");
            }

            if (string.IsNullOrWhiteSpace(githubProvider.ProviderUsername))
            {
                throw new InvalidOperationException("GitHub username was not found");
            }

            List<GitHubRepositorySyncDto> githubRepos = await _gitHubApiClient
                .GetPublicRepositoriesAsync(githubProvider.ProviderUsername);

            var githubRepoIds = githubRepos
                .Select(x => x.GithubRepoId)
                .ToList();

            var existingRepos = await _dbContext.Repositories
                .Where(x => x.UserId == userId && githubRepoIds.Contains(x.GithubRepoId))
                .ToListAsync();

            foreach (var githubRepo in githubRepos)
            {
                var existingRepo = existingRepos
                    .FirstOrDefault(x => x.GithubRepoId == githubRepo.GithubRepoId);

                if (existingRepo == null)
                {
                    var repo = new Repository
                    {
                        UserId = userId,
                        GithubRepoId = githubRepo.GithubRepoId,
                        Name = githubRepo.Name,
                        FullName = githubRepo.FullName,
                        HtmlUrl = githubRepo.HtmlUrl,
                        Description = githubRepo.Description,
                        PrimaryLanguage = githubRepo.PrimaryLanguage,
                        Stars = githubRepo.Stars,
                        Forks = githubRepo.Forks,
                        IsPrivate = false,
                        IsSelectedForPortfolio = true,
                        GithubCreatedAt = githubRepo.GithubCreatedAt,
                        GithubUpdatedAt = githubRepo.GithubUpdatedAt,
                        SyncedAt = DateTime.UtcNow
                    };

                    _dbContext.Repositories.Add(repo);
                }
                else
                {
                    existingRepo.Name = githubRepo.Name;
                    existingRepo.FullName = githubRepo.FullName;
                    existingRepo.HtmlUrl = githubRepo.HtmlUrl;
                    existingRepo.Description = githubRepo.Description;
                    existingRepo.PrimaryLanguage = githubRepo.PrimaryLanguage;
                    existingRepo.Stars = githubRepo.Stars;
                    existingRepo.Forks = githubRepo.Forks;
                    existingRepo.IsPrivate = false;
                    existingRepo.GithubCreatedAt = githubRepo.GithubCreatedAt;
                    existingRepo.GithubUpdatedAt = githubRepo.GithubUpdatedAt;
                    existingRepo.SyncedAt = DateTime.UtcNow;
                }

            }
            await _dbContext.SaveChangesAsync();

            return await GetSavedRepositoriesAsync(userId);
        }

        public async Task<List<GitHubRepositoryResponseDto>> GetSavedRepositoriesAsync(Guid userId)
        {
            var repos = await _dbContext.Repositories
                .AsNoTracking()
                .Include(x => x.RepoInsight)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.GithubUpdatedAt)
                .ToListAsync();

            return repos.Select(x => ToRepositoryDto(x, includeAllInsightStatuses: true)).ToList();
        }

        public static GitHubRepositoryResponseDto ToRepositoryDto(
            Repository repository,
            bool includeAllInsightStatuses)
        {
            var insight = includeAllInsightStatuses
                ? repository.RepoInsight
                : repository.RepoInsight?.AnalysisStatus == "completed"
                    ? repository.RepoInsight
                    : null;

            return new GitHubRepositoryResponseDto
            {
                RepositoryId = repository.RepositoryId,
                Name = repository.Name,
                FullName = repository.FullName,
                HtmlUrl = repository.HtmlUrl,
                Description = repository.Description,
                PrimaryLanguage = repository.PrimaryLanguage,
                Stars = repository.Stars,
                Forks = repository.Forks,
                IsSelectedForPortfolio = repository.IsSelectedForPortfolio,
                SyncedAt = repository.SyncedAt,
                Insight = insight == null ? null : RepoInsightService.ToDto(insight)
            };
        }
    }
}
