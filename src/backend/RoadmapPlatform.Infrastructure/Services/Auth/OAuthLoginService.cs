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

/// <summary>
/// Implements OAuth login for external authentication providers.
/// </summary>
/// <remarks>
/// This service is used after an external provider, such as Google or GitHub,
/// has already authenticated the user.
///
/// It either:
/// - Finds an existing user linked to the external provider account.
/// - Or creates a new user, profile, auth provider, and default Learner role.
/// </remarks>
public class OAuthLoginService : IOAuthLoginService
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Creates a new OAuth login service.
    /// </summary>
    /// <param name="dbContext">
    /// The application database context.
    /// </param>
    public OAuthLoginService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Logs in an existing user or creates a new user from OAuth user information.
    /// </summary>
    /// <param name="externalLogin">
    /// The external OAuth user information returned by a provider such as Google or GitHub.
    /// </param>
    /// <returns>
    /// The authenticated application user data used by AuthService to generate a JWT.
    /// </returns>
    /// <remarks>
    /// This method does not generate the JWT token directly.
    /// It only returns the authenticated user information.
    /// </remarks>
    public async Task<AuthenticatedUserDto> LoginOrCreateUserAsync(OAuthUserInfoDto externalLogin)
    {
        var provider = externalLogin.Provider;
        var providerUserId = externalLogin.ProviderUserId;
        var providerUsername = externalLogin.ProviderUsername;
        var displayName = externalLogin.DisplayName;
        var email = externalLogin.Email?.Trim().ToLowerInvariant();
        var accessToken = NormalizeNullable(externalLogin.AccessToken);

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException("External provider was not provided");
        }

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            throw new InvalidOperationException($"{externalLogin.Provider} ID was not provided");
        }

        // Try to find an existing application account linked to this provider account.
        var existingProvider = await _dbContext.UserAuthProviders
            .Include(x => x.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x =>
                x.Provider == provider &&
                x.ProviderUserId == providerUserId);

        if (existingProvider?.User != null)
        {
            // Refresh provider metadata on each successful OAuth login.
            existingProvider.ProviderUsername = providerUsername;

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                existingProvider.AccessToken = accessToken;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                existingProvider.Email = email;

                // Google email is treated as verified when provided by Google.
                if (provider == AuthProviders.Google && existingProvider.EmailVerifiedAt == null)
                {
                    existingProvider.EmailVerifiedAt = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();

            // Repair old or incomplete accounts that have no assigned roles.
            await EnsureDefaultLearnerRoleIfNoRolesAsync(existingProvider.User);

            return ToAuthenticatedUserDto(existingProvider.User, existingProvider.Email);
        }

        // If another account already uses this email, do not auto-link it.
        // The user must log in to that account and link the provider manually.
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

        // Choose the best available base value for generating an internal username.
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
            AccessToken = accessToken,
            EmailVerifiedAt = isEmailVerifiedByProvider ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserAuthProviders.Add(externalProvider);

        var learnerRole = await GetRequiredLearnerRoleAsync();

        var learnerUserRole = new UserRole
        {
            User = user,
            UserId = user.UserId,
            Role = learnerRole,
            RoleId = learnerRole.RoleId
        };

        _dbContext.UserRoles.Add(learnerUserRole);
        user.UserRoles.Add(learnerUserRole);

        await _dbContext.SaveChangesAsync();

        return ToAuthenticatedUserDto(user, externalProvider.Email);
    }

    /// <summary>
    /// Ensures that a user has the default Learner role when no roles are assigned.
    /// </summary>
    /// <remarks>
    /// This is mainly a safety repair for old or incomplete accounts.
    /// New OAuth-created accounts are already assigned the Learner role during creation.
    /// </remarks>
    private async Task EnsureDefaultLearnerRoleIfNoRolesAsync(User user)
    {
        if (user.UserRoles.Any())
        {
            return;
        }

        var learnerRole = await GetRequiredLearnerRoleAsync();

        var userRole = new UserRole
        {
            User = user,
            UserId = user.UserId,
            Role = learnerRole,
            RoleId = learnerRole.RoleId
        };

        _dbContext.UserRoles.Add(userRole);
        user.UserRoles.Add(userRole);

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the default Learner role from the database.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Learner role is missing from the database.
    /// </exception>
    private async Task<Role> GetRequiredLearnerRoleAsync()
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.RoleName == RoleNames.Learner)
            ?? throw new InvalidOperationException(
                "Default learner role was not found. Run the RBAC role-permission seed before creating or logging in learner accounts.");
    }

    /// <summary>
    /// Maps a User entity into an authenticated user DTO.
    /// </summary>
    /// <param name="user">
    /// The authenticated user entity.
    /// </param>
    /// <param name="email">
    /// The email associated with the authenticated provider.
    /// </param>
    /// <returns>
    /// The authenticated user DTO used for JWT creation.
    /// </returns>
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

    /// <summary>
    /// Generates a unique internal username from an external provider username,
    /// display name, or email prefix.
    /// </summary>
    /// <param name="baseUsername">
    /// The raw base username value.
    /// </param>
    /// <returns>
    /// A unique username and its normalized version.
    /// </returns>
    /// <remarks>
    /// The generated username is cleaned, lowercased, limited in length,
    /// and appended with a random numeric suffix.
    /// </remarks>
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

    /// <summary>
    /// Trims an optional string and converts empty values to null.
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    /// <summary>
    /// Removes Vietnamese and other diacritic marks from a string.
    /// </summary>
    /// <param name="value">
    /// The original input value.
    /// </param>
    /// <returns>
    /// The input value without diacritic marks.
    /// </returns>
    private static string RemoveDiacritics(string value)
    {
        value = value.Replace("đ", "d").Replace("Đ", "D");

        var normalized = value.Normalize(NormalizationForm.FormD);

        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normalizes a username candidate so it can be used as an internal username base.
    /// </summary>
    /// <param name="username">
    /// The raw username candidate.
    /// </param>
    /// <returns>
    /// A cleaned username base.
    /// </returns>
    /// <remarks>
    /// This method:
    /// - Trims the value.
    /// - Converts it to lowercase.
    /// - Keeps only letters, digits, dot, underscore, and hyphen.
    /// - Falls back to "user" when the result is empty.
    /// - Limits the base username to 30 characters.
    /// </remarks>
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
