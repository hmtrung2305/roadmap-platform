using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Models.GitHub;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Services.GitHub;

public sealed class GitHubTokenService : IGitHubTokenService
{
    private readonly ApplicationDbContext _dbContext;

    public GitHubTokenService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GitHubAccessTokenContext> GetRequiredAccessTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var githubProvider = await _dbContext.UserAuthProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Provider == AuthProviders.GitHub,
                cancellationToken);

        if (githubProvider == null)
        {
            throw GitHubIntegrationException.NotLinked();
        }

        if (string.IsNullOrWhiteSpace(githubProvider.ProviderUsername) ||
            string.IsNullOrWhiteSpace(githubProvider.AccessToken))
        {
            throw GitHubIntegrationException.TokenMissing();
        }

        return new GitHubAccessTokenContext
        {
            Username = githubProvider.ProviderUsername,
            AccessToken = githubProvider.AccessToken
        };
    }
}
