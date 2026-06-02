using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.DTOs.Portfolio;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Services.Portfolio
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _dbContext;

        public PortfolioService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PortfolioResponseDto> GetMyPortfolioAsync(Guid userId)
        {
            return await BuildPortfolioResponseAsync(userId, false);
        }

        public async Task<PortfolioResponseDto> GetPortfolioByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Username was not provided");
            }

            var usernameNormalized = username.Trim().ToLower();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UsernameNormalized == usernameNormalized);

            if (user == null)
            {
                throw new NotFoundException("Portfolio was not found");
            }

            return await BuildPortfolioResponseAsync(user.UserId, true);
        }

        public async Task<PortfolioResponseDto> UpdatePortfolioRepositoriesAsync(Guid userId, UpdatePortfolioRepositoriesRequestDto request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body was not provided");
            }

            if (request.RepositoryIds == null)
            {
                throw new InvalidOperationException("RepositoryIds was not provided");
            }

            var selectedRepositoryIds = request.RepositoryIds
                .Distinct().ToHashSet();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            var savedRepositories = await _dbContext.Repositories
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var savedRepositoryIds = savedRepositories
                .Select(x => x.RepositoryId)
                .ToHashSet();

            var invalidRepositories = selectedRepositoryIds
                .Where(id => !savedRepositoryIds.Contains(id))
                .ToList();

            if (invalidRepositories.Any())
            {
                throw new InvalidOperationException("" +
                    "One or more repositories seleced do not belong to the current user");
            }

            foreach (var repository in savedRepositories)
            {
                repository.IsSelectedForPortfolio =
                    selectedRepositoryIds.Contains(repository.RepositoryId);
            }

            await _dbContext.SaveChangesAsync();

            return await BuildPortfolioResponseAsync(userId, false);
        }

        private async Task<PortfolioResponseDto> BuildPortfolioResponseAsync(Guid userId, bool requirePublicProfile)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new InvalidOperationException("User was not found");
            }

            var profile = await _dbContext.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
            {
                throw new NotFoundException("User profile was not found");
            }

            if (requirePublicProfile && !profile.IsPublic)
            {
                throw new NotFoundException("Portfolio was not found");
            }

            var repositories = await _dbContext.Repositories
                .Where(x =>
                    x.UserId == userId &&
                    x.IsSelectedForPortfolio &&
                    !x.IsPrivate)
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
                })
                .ToListAsync();

            return new PortfolioResponseDto
            {
                DisplayName = profile.DisplayName,
                Headline = profile.Headline,
                Bio = profile.Bio,
                Location = profile.Location,
                AvatarUrl = profile.AvatarUrl,
                CoverImageUrl = profile.CoverImageUrl,
                CareerGoal = profile.CareerGoal,
                CurrentRole = profile.CurrentRole,
                PublicEmail = profile.PublicEmail,
                GithubUrl = profile.GithubUrl,
                LinkedinUrl = profile.LinkedinUrl,
                PersonalWebsiteUrl = profile.PersonalWebsiteUrl,
                Repositories = repositories
            };
        }
    }
}