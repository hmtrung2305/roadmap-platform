using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users
{
    /// <summary>
    /// Provides current-user account and profile operations.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets the authenticated user's account information, roles, and effective permissions.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <returns>The current user's account, role, and permission information.</returns>
        /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
        public async Task<CurrentUserResponseDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new
                {
                    x.UserId,
                    x.Username,
                    x.Status,
                    Email = x.UserAuthProviders
                        .OrderByDescending(provider => provider.Provider == "local")
                        .ThenBy(provider => provider.Provider)
                        .Select(provider => provider.Email)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            var roles = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .Select(userRole => userRole.Role.RoleName)
                .Distinct()
                .OrderBy(roleName => roleName)
                .ToListAsync();

            var permissions = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .SelectMany(userRole => userRole.Role.PermissionRoles
                    .Select(permissionRole => permissionRole.Permission.PermissionName))
                .Distinct()
                .OrderBy(permissionName => permissionName)
                .ToListAsync();

            return new CurrentUserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Status = user.Status,
                Roles = roles,
                Permissions = permissions
            };
        }

        /// <summary>
        /// Updates the authenticated user's account-level information.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <param name="request">The account update request.</param>
        /// <returns>The updated user account information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the request or username is invalid.</exception>
        /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
        /// <exception cref="ConflictException">Thrown when the username already exists.</exception>
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

        /// <summary>
        /// Gets the authenticated user's profile.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <returns>The authenticated user's profile information.</returns>
        /// <exception cref="NotFoundException">Thrown when the user or profile does not exist.</exception>
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

        /// <summary>
        /// Updates the authenticated user's profile fields.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <param name="request">The profile update request.</param>
        /// <returns>The updated profile information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the request body is missing.</exception>
        /// <exception cref="NotFoundException">Thrown when the profile does not exist.</exception>
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

        /// <summary>
        /// Soft-deletes the authenticated user's account.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
        /// <exception cref="ConflictException">Thrown when the account is already deleted.</exception>
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

        /// <summary>
        /// Maps a user account to a user response DTO and resolves the preferred email address.
        /// </summary>
        /// <param name="userId">The user's identifier.</param>
        /// <param name="username">The user's username.</param>
        /// <param name="status">The user's account status.</param>
        /// <returns>The mapped user response DTO.</returns>
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

        /// <summary>
        /// Maps a user profile entity to a profile response DTO.
        /// </summary>
        /// <param name="profile">The user profile entity.</param>
        /// <returns>The mapped profile response DTO.</returns>
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

        /// <summary>
        /// Trims a string value and converts blank values to null.
        /// </summary>
        /// <param name="value">The input string value.</param>
        /// <returns>The trimmed value, or null when the value is blank.</returns>
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