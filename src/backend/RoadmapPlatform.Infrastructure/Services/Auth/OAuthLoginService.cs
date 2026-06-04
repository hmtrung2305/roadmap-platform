using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Auth;

public class OAuthLoginService : IOAuthLoginService
{
    private readonly ApplicationDbContext _dbContext;

    public OAuthLoginService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthenticatedUserDto> LoginOrCreateUserAsync(OAuthUserInfoDto externalLogin)
    {
        var provider = externalLogin.Provider;
        var providerUserId = externalLogin.ProviderUserId;
        var providerUsername = externalLogin.ProviderUsername;
        var displayName = externalLogin.DisplayName;
        var email = externalLogin.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException("External provider was not provided");
        }

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            throw new InvalidOperationException($"{externalLogin.Provider} ID was not provided");
        }

        var existingProvider = await _dbContext.UserAuthProviders
            .Include(x => x.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x =>
                x.Provider == provider &&
                x.ProviderUserId == providerUserId);

        if (existingProvider?.User != null)
        {
            existingProvider.ProviderUsername = providerUsername;

            if (!string.IsNullOrWhiteSpace(email))
            {
                existingProvider.Email = email;

                if (provider == AuthProviders.Google && existingProvider.EmailVerifiedAt == null)
                {
                    existingProvider.EmailVerifiedAt = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();

            return ToAuthenticatedUserDto(existingProvider.User, existingProvider.Email);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            bool emailExists = await _dbContext.UserAuthProviders
                .AnyAsync(x => x.Email == email);

            if (emailExists)
            {
                throw new ConflictException(
                    "An account with this email already exists. Please log in and link this provider manually.");
            }
        }

        var baseUsername = providerUsername ??
            displayName ??
            email?.Split("@")[0] ??
            "user";

        var generatedUsername = await GenerateUniqueUsername(baseUsername);

        var user = new User
        {
            Username = generatedUsername.Username,
            UsernameNormalized = generatedUsername.UsernameNormalized,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        string? githubUrl = null;
        if (provider == AuthProviders.GitHub && !string.IsNullOrWhiteSpace(providerUsername))
        {
            githubUrl = $"https://github.com/{providerUsername}";
        }

        var profile = new UserProfile
        {
            User = user,
            DisplayName = displayName ?? providerUsername ?? generatedUsername.Username,
            GithubUrl = githubUrl,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _dbContext.UserProfiles.Add(profile);

        bool isEmailVerifiedByProvider =
            provider == AuthProviders.Google &&
            !string.IsNullOrWhiteSpace(email);

        var externalProvider = new UserAuthProvider
        {
            User = user,
            Email = email,
            PendingEmail = null,
            Provider = provider,
            ProviderUserId = providerUserId,
            ProviderUsername = providerUsername,
            EmailVerifiedAt = isEmailVerifiedByProvider ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserAuthProviders.Add(externalProvider);

        var learnerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.RoleName == RoleNames.Learner);

        if (learnerRole != null)
        {
            var userRole = new UserRole
            {
                User = user,
                RoleId = learnerRole.RoleId,
                Role = learnerRole
            };
            _dbContext.UserRoles.Add(userRole);
        }
        
        await _dbContext.SaveChangesAsync();

        return ToAuthenticatedUserDto(user, externalProvider.Email);
    }

    private static AuthenticatedUserDto ToAuthenticatedUserDto(User user, string? email)
    {
        return new AuthenticatedUserDto
        {
            UserId = user.UserId,
            Username = user.Username ?? string.Empty,
            Email = email,
            Status = user.Status,
            Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.RoleName)
                    .ToList(),
        };
    }

    private async Task<(string Username, string UsernameNormalized)> GenerateUniqueUsername(string baseUsername)
    {
        var cleanedBaseUsername = NormalizeUsername(RemoveDiacritics(baseUsername));

        while (true)
        {
            var suffix = Random.Shared.Next(1000, 9999);
            var username = $"{cleanedBaseUsername}{suffix}";
            var usernameNormalized = username.ToLowerInvariant();

            var exists = await _dbContext.Users
                .AnyAsync(x => x.UsernameNormalized == usernameNormalized);

            if (!exists)
            {
                return (username, usernameNormalized);
            }
        }
    }

    private static string RemoveDiacritics(string value)
    {
        value = value.Replace("đ", "d").Replace("Đ", "D");

        var normalized = value.Normalize(NormalizationForm.FormD);

        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    private static string NormalizeUsername(string username)
    {
        username = username.Trim().ToLowerInvariant();

        var cleaned = new string(username
            .Where(c =>
                char.IsLetterOrDigit(c) ||
                c == '.' ||
                c == '_' ||
                c == '-')
            .ToArray());

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "user";
        }

        if (cleaned.Length > 30)
        {
            cleaned = cleaned[..30];
        }

        return cleaned;
    }
}
