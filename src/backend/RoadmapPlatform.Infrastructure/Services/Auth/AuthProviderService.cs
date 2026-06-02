using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Security.Claims;

namespace RoadmapPlatform.Infrastructure.Services.Auth
{
    public class AuthProviderService : IAuthProviderService
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthProviderService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<LoginMethodStatusDto>> GetAuthProviderStatusAsync(Guid userId)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var linkedProviders = await _dbContext.UserAuthProviders
                .Where(x => x.UserId == userId)
                .Select(x => x.Provider)
                .ToListAsync();

            var linkedProviderCount = linkedProviders
                .Distinct()
                .Count();

            var hasLocal = linkedProviders.Contains(AuthProviders.Local);
            var hasGoogle = linkedProviders.Contains(AuthProviders.Google);
            var hasGitHub = linkedProviders.Contains(AuthProviders.GitHub);

            return new List<LoginMethodStatusDto>
        {
            new LoginMethodStatusDto
            {
                Provider = AuthProviders.Local,
                DisplayName = "Local",
                IsLinked = hasLocal,
                CanUnlink = hasLocal && linkedProviderCount > 1
            },
            new LoginMethodStatusDto
            {
                Provider = AuthProviders.Google,
                DisplayName = "Google",
                IsLinked = hasGoogle,
                CanUnlink = hasGoogle && linkedProviderCount > 1
            },
            new LoginMethodStatusDto
            {
                Provider = AuthProviders.GitHub,
                DisplayName = "GitHub",
                IsLinked = hasGitHub,
                CanUnlink = hasGitHub && linkedProviderCount > 1
            }
        };
        }

        public async Task LinkGitHubAsync(Guid userId, ClaimsPrincipal githubUser)
        {
            var githubUserId = githubUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var githubEmail = githubUser.FindFirstValue(ClaimTypes.Email);
            var githubUsername = githubUser.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(githubUserId))
            {
                throw new InvalidOperationException("GitHub ID was not provided");
            }

            if (string.IsNullOrWhiteSpace(githubUsername))
            {
                throw new InvalidOperationException("GitHub username was not provided");
            }

            await LinkExternalProviderAsync(
                userId: userId,
                provider: AuthProviders.GitHub,
                providerUserId: githubUserId,
                email: githubEmail,
                providerUsername: githubUsername,
                emailVerifiedAt: githubEmail == null ? null : DateTime.UtcNow);
        }

        public async Task LinkGoogleAsync(Guid userId, ClaimsPrincipal googleUser)
        {
            var googleUserId = googleUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var googleEmail = googleUser.FindFirstValue(ClaimTypes.Email);
            var googleUsername = googleUser.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(googleUserId))
            {
                throw new InvalidOperationException("Google ID was not provided");
            }

            if (string.IsNullOrWhiteSpace(googleEmail))
            {
                throw new InvalidOperationException("Google email was not provided");
            }

            await LinkExternalProviderAsync(
                userId: userId,
                provider: AuthProviders.Google,
                providerUserId: googleUserId,
                email: googleEmail,
                providerUsername: googleUsername,
                emailVerifiedAt: DateTime.UtcNow);
        }

        public async Task UnlinkProviderAsync(Guid userId, string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new InvalidOperationException("Provider was not provided");
            }

            provider = AuthProviders.Normalize(provider);

            if (!AuthProviders.IsSupported(provider))
            {
                throw new InvalidOperationException("Unsupported authentication provider");
            }

            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var linkedProviders = await _dbContext.UserAuthProviders
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var providerToRemove = linkedProviders
                .FirstOrDefault(x => x.Provider == provider);

            if (providerToRemove == null)
            {
                throw new NotFoundException("This login method is not linked to your account");
            }

            var linkedProviderCount = linkedProviders
                .Select(x => x.Provider)
                .Distinct()
                .Count();

            if (linkedProviderCount <= 1)
            {
                throw new InvalidOperationException("You cannot unlink your only login method");
            }

            _dbContext.UserAuthProviders.Remove(providerToRemove);

            await _dbContext.SaveChangesAsync();
        }

        private async Task LinkExternalProviderAsync(
            Guid userId,
            string provider,
            string providerUserId,
            string? email,
            string? providerUsername,
            DateTime? emailVerifiedAt)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var alreadyLinkedToCurrentUser = await _dbContext.UserAuthProviders
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.Provider == provider);

            if (alreadyLinkedToCurrentUser)
            {
                throw new ConflictException($"This account already has {provider} linked");
            }

            var existingProviderAccount = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ProviderUserId == providerUserId);

            if (existingProviderAccount != null)
            {
                throw new ConflictException($"This {provider} account is already linked to another user");
            }

            var now = DateTime.UtcNow;

            var authProvider = new UserAuthProvider
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProviderUsername = NormalizeNullable(providerUsername),
                Email = NormalizeNullable(email),
                PendingEmail = null,
                PasswordHash = null,
                EmailVerifiedAt = emailVerifiedAt,
                CreatedAt = now
            };

            _dbContext.UserAuthProviders.Add(authProvider);

            await _dbContext.SaveChangesAsync();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
