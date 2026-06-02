using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.DTOs.Portfolio;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Application.Interfaces.Portfolio;

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
            };
        }
    }
}