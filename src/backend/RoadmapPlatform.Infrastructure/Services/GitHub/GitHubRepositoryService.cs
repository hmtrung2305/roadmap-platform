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
            // Find the user's linked GitHub account
            // Used to get the GitHub username required for the GitHub API call
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

            // Fetch latest public repositories directly from GitHub API
            List<GitHubRepositorySyncDto> githubRepos = await _gitHubApiClient
                .GetPublicRepositoriesAsync(githubProvider.ProviderUsername);

            // Extract GitHub repository IDs
            // Used to compare fetched GitHub repos with repos already stored in the database
            var githubRepoIds = githubRepos
                .Select(x => x.GithubRepoId)
                .ToList();

            // Load repositories already stored in the database
            var existingRepos = await _dbContext.Repositories
                .Where(x => x.UserId == userId && githubRepoIds.Contains(x.GithubRepoId))
                .ToListAsync();

            // Synchronize GitHub repositories into the database.
            foreach (var githubRepo in githubRepos)
            {
                // Try to find matching repository already stored locally.
                var existingRepo = existingRepos
                    .FirstOrDefault(x => x.GithubRepoId == githubRepo.GithubRepoId);

                // Repository doesn't exist in the database yet -> create a new row
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
                    // Repository already exists locally -> update the exisiting repo with the latest GitHub information
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
            // Convert database entities into response DTO.
            var repos = await _dbContext.Repositories
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.GithubUpdatedAt)
                .Select(x => new GitHubRepositoryResponseDto
                {
                    RepositoryId = x.RepositoryId,
                    Name = x.Name,
                    FullName = x.FullName,
                    HtmlUrl = x.HtmlUrl,
                    Description = x.Description,
                    PrimaryLanguage = x.PrimaryLanguage,
                    Stars = x.Stars,
                    Forks = x.Forks,
                    IsSelectedForPortfolio = x.IsSelectedForPortfolio,
                    SyncedAt = x.SyncedAt
                }).ToListAsync();

            return repos;
        }
    }
}
