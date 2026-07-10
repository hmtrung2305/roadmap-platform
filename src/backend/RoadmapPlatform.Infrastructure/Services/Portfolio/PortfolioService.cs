using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.DTOs.Portfolio;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Services.GitHub;

namespace RoadmapPlatform.Infrastructure.Services.Portfolio
{
    /// <summary>
    /// Provides portfolio operations for the authenticated user and public portfolio viewers.
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioService"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        public PortfolioService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets the authenticated user's portfolio.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <returns>The authenticated user's portfolio.</returns>
        public async Task<PortfolioResponseDto> GetMyPortfolioAsync(Guid userId)
        {
            return await BuildPortfolioResponseAsync(userId, false);
        }

        /// <summary>
        /// Gets a public portfolio by username.
        /// </summary>
        /// <param name="username">The username of the portfolio owner.</param>
        /// <returns>The public portfolio for the requested username.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the username is missing.</exception>
        /// <exception cref="NotFoundException">Thrown when the user or public portfolio cannot be found.</exception>
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

        /// <summary>
        /// Updates the authenticated user's selected portfolio repositories.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <param name="request">The selected repository update request.</param>
        /// <returns>The updated portfolio.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the request is invalid or contains repositories not owned by the user.</exception>
        /// <exception cref="ConflictException">Thrown when more than six repositories are selected.</exception>
        /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
        public async Task<PortfolioResponseDto> UpdatePortfolioRepositoriesAsync(
            Guid userId,
            UpdatePortfolioRepositoriesRequestDto request)
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

            if (selectedRepositoryIds.Count > 6)
            {
                throw new ConflictException("You can select up to 6 repositories for your portfolio");
            }

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
                throw new InvalidOperationException(
                    "One or more selected repositories do not belong to the current user");
            }

            foreach (var repository in savedRepositories)
            {
                repository.IsSelectedForPortfolio =
                    selectedRepositoryIds.Contains(repository.RepositoryId);
            }

            await _dbContext.SaveChangesAsync();

            return await BuildPortfolioResponseAsync(userId, false);
        }

        /// <summary>
        /// Builds a portfolio response for either the authenticated user or a public viewer.
        /// </summary>
        /// <param name="userId">The portfolio owner's user identifier.</param>
        /// <param name="requirePublicProfile">Whether the profile must be public.</param>
        /// <returns>The portfolio response.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user does not exist.</exception>
        /// <exception cref="NotFoundException">Thrown when the profile does not exist or is not public when required.</exception>
        private async Task<PortfolioResponseDto> BuildPortfolioResponseAsync(
            Guid userId,
            bool requirePublicProfile)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("Portfolio was not found"); // or User was not found
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
                .AsNoTracking()
                .Include(x => x.RepoInsight)
                .Where(x =>
                    x.UserId == userId &&
                    x.IsSelectedForPortfolio &&
                    !x.IsPrivate)
                .OrderByDescending(x => x.GithubUpdatedAt)
                .Take(6)
                .ToListAsync();

            var repositoryDtos = repositories
                .Select(repository => GitHubRepositoryService.ToRepositoryDto(
                    repository,
                    includeAllInsightStatuses: false))
                .ToList();

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
                Repositories = repositoryDtos
            };
        }
    }
}