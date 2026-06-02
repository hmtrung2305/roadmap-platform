using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserResponseDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            return await MapToUserResponseAsync(user.UserId, user.Username, user.Status);
        }

        public async Task<UserResponseDto> UpdateCurrentUserAsync(Guid userId, UpdateCurrentUserRequestDto request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body was not provided");
            }

            var username = request.Username?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Username was not provided");
            }

            var usernameNormalized = username.ToLowerInvariant();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            var usernameChanged = !string.Equals(
                user.UsernameNormalized,
                usernameNormalized,
                StringComparison.Ordinal);

            if (usernameChanged)
            {
                var usernameExists = await _dbContext.Users
                    .AnyAsync(x =>
                        x.UserId != userId &&
                        x.UsernameNormalized == usernameNormalized);

                if (usernameExists)
                {
                    throw new ConflictException("Username already exists");
                }

                user.UsernameNormalized = usernameNormalized;
            }

            user.Username = username;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return await MapToUserResponseAsync(user.UserId, user.Username, user.Status);
        }

        public async Task<ProfileResponseDto> GetMyProfileAsync(Guid userId)
        {
            var userExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var profile = await _dbContext.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
            {
                throw new NotFoundException("User profile was not found");
            }

            return MapToProfileResponse(profile);
        }

        public async Task<ProfileResponseDto> UpdateMyProfileAsync(
            Guid userId,
            UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body was not provided");
            }

            var profile = await _dbContext.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
            {
                throw new NotFoundException("User profile was not found");
            }

            if (request.DisplayName != null)
            {
                profile.DisplayName = TrimOrNull(request.DisplayName);
            }

            if (request.Headline != null)
            {
                profile.Headline = TrimOrNull(request.Headline);
            }

            if (request.Bio != null)
            {
                profile.Bio = TrimOrNull(request.Bio);
            }

            if (request.Location != null)
            {
                profile.Location = TrimOrNull(request.Location);
            }

            if (request.AvatarUrl != null)
            {
                profile.AvatarUrl = TrimOrNull(request.AvatarUrl);
            }

            if (request.CoverImageUrl != null)
            {
                profile.CoverImageUrl = TrimOrNull(request.CoverImageUrl);
            }

            if (request.CareerGoal != null)
            {
                profile.CareerGoal = TrimOrNull(request.CareerGoal);
            }

            if (request.CurrentRole != null)
            {
                profile.CurrentRole = TrimOrNull(request.CurrentRole);
            }

            if (request.PublicEmail != null)
            {
                profile.PublicEmail = TrimOrNull(request.PublicEmail);
            }

            if (request.GithubUrl != null)
            {
                profile.GithubUrl = TrimOrNull(request.GithubUrl);
            }

            if (request.LinkedinUrl != null)
            {
                profile.LinkedinUrl = TrimOrNull(request.LinkedinUrl);
            }

            if (request.PersonalWebsiteUrl != null)
            {
                profile.PersonalWebsiteUrl = TrimOrNull(request.PersonalWebsiteUrl);
            }

            if (request.IsPublic.HasValue)
            {
                profile.IsPublic = request.IsPublic.Value;
            }

            profile.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return MapToProfileResponse(profile);
        }

        public async Task DeleteAccountAsync(Guid userId)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            if (user.Status == UserStatuses.Deleted)
            {
                throw new ConflictException("Account is already deleted");
            }

            user.Status = UserStatuses.Deleted;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        private async Task<UserResponseDto> MapToUserResponseAsync(Guid userId, string username, string status)
        {
            var email = await _dbContext.UserAuthProviders
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Provider == "local")
                .ThenBy(x => x.Provider)
                .Select(x => x.Email)
                .FirstOrDefaultAsync();

            return new UserResponseDto
            {
                UserId = userId,
                Username = username,
                Email = email,
                Status = status
            };
        }

        private static ProfileResponseDto MapToProfileResponse(UserProfile profile)
        {
            return new ProfileResponseDto
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
                IsPublic = profile.IsPublic
            };
        }

        private static string? TrimOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}