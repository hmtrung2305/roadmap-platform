using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailVerificationService _emailVerificationService;

        public AuthProviderService(
            ApplicationDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            IEmailVerificationService emailVerificationService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _emailVerificationService = emailVerificationService;
        }

        public async Task<List<LoginMethodStatusDto>> GetAuthProviderStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId, cancellationToken);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var linkedProviders = await _dbContext.UserAuthProviders
                .Where(x => x.UserId == userId)
                .Select(x => x.Provider)
                .ToListAsync(cancellationToken);

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

        public async Task LinkLocalLoginAsync(
            Guid userId,
            LinkLocalLoginRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body was not provided");
            }

            var email = NormalizeEmailOrThrow(request.Email);

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new InvalidOperationException("Password was not provided");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException("User was not found");
            }

            var existingLocalProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(
                    x => x.UserId == userId &&
                         x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (existingLocalProvider != null)
            {
                if (existingLocalProvider.EmailVerifiedAt == null)
                {
                    throw new ConflictException("This account already has a pending local login verification");
                }

                throw new ConflictException("This account already has a local login method");
            }

            var emailAlreadyRegistered = await _dbContext.UserAuthProviders
                .AnyAsync(
                    x => x.Provider == AuthProviders.Local &&
                         x.ProviderUserId == email,
                    cancellationToken);

            if (emailAlreadyRegistered)
            {
                throw new ConflictException("Email is already registered");
            }

            var now = DateTime.UtcNow;

            var localProvider = new UserAuthProvider
            {
                UserId = userId,
                Provider = AuthProviders.Local,
                ProviderUserId = email,
                ProviderUsername = null,
                Email = email,
                PendingEmail = null,
                PasswordHash = _passwordHasher.HashPassword(user, request.Password),
                EmailVerifiedAt = null,
                CreatedAt = now
            };

            _dbContext.UserAuthProviders.Add(localProvider);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _emailVerificationService.SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                email,
                EmailVerificationPurposes.LinkLocal,
                cancellationToken);
        }

        public async Task ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body was not provided");
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                throw new InvalidOperationException("Current password was not provided");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new InvalidOperationException("New password was not provided");
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                throw new InvalidOperationException("New password must be different from the current password");
            }

            var localProvider = await _dbContext.UserAuthProviders
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId &&
                         x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null ||
                localProvider.User == null ||
                string.IsNullOrWhiteSpace(localProvider.PasswordHash))
            {
                throw new InvalidOperationException("This account does not have a local password login");
            }

            if (localProvider.EmailVerifiedAt == null)
            {
                throw new InvalidOperationException("Local email must be verified before changing password");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                localProvider.User,
                localProvider.PasswordHash,
                request.CurrentPassword);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Current password is incorrect");
            }

            localProvider.PasswordHash = _passwordHasher.HashPassword(
                localProvider.User,
                request.NewPassword);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LinkGitHubAsync(
            Guid userId,
            ClaimsPrincipal githubUser,
            CancellationToken cancellationToken = default)
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
                emailVerifiedAt: githubEmail == null ? null : DateTime.UtcNow,
                cancellationToken: cancellationToken);
        }

        public async Task LinkGoogleAsync(
            Guid userId,
            ClaimsPrincipal googleUser,
            CancellationToken cancellationToken = default)
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
                emailVerifiedAt: DateTime.UtcNow,
                cancellationToken: cancellationToken);
        }

        public async Task UnlinkProviderAsync(
            Guid userId,
            string provider,
            CancellationToken cancellationToken = default)
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
                .AnyAsync(x => x.UserId == userId, cancellationToken);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var linkedProviders = await _dbContext.UserAuthProviders
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

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

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task LinkExternalProviderAsync(
            Guid userId,
            string provider,
            string providerUserId,
            string? email,
            string? providerUsername,
            DateTime? emailVerifiedAt,
            CancellationToken cancellationToken = default)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId, cancellationToken);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var alreadyLinkedToCurrentUser = await _dbContext.UserAuthProviders
                .AnyAsync(
                    x => x.UserId == userId &&
                         x.Provider == provider,
                    cancellationToken);

            if (alreadyLinkedToCurrentUser)
            {
                throw new ConflictException($"This account already has {provider} linked");
            }

            var existingProviderAccount = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(
                    x => x.Provider == provider &&
                         x.ProviderUserId == providerUserId,
                    cancellationToken);

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

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeEmailOrThrow(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email was not provided");
            }

            return email.Trim().ToLowerInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
