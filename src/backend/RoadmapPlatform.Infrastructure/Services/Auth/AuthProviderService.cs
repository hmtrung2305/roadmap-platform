using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.Auth
{
    /// <summary>
    /// Implements account-level authentication provider management.
    /// </summary>
    /// <remarks>
    /// This service manages login methods for existing users, such as:
    /// - Checking linked login provider status.
    /// - Linking local email/password login.
    /// - Changing local password.
    /// - Linking Google login.
    /// - Linking GitHub login.
    /// - Unlinking login providers.
    ///
    /// This service is different from AuthService.
    /// AuthService handles registration and login.
    /// AuthProviderService handles login methods after an account already exists.
    /// </remarks>
    public class AuthProviderService : IAuthProviderService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailVerificationService _emailVerificationService;

        /// <summary>
        /// Creates a new authentication provider service.
        /// </summary>
        public AuthProviderService(
            ApplicationDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            IEmailVerificationService emailVerificationService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _emailVerificationService = emailVerificationService;
        }

        /// <summary>
        /// Gets the current linking status of all supported login providers for a user.
        /// </summary>
        /// <param name="userId">
        /// The user ID whose login provider status should be loaded.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A list containing local, Google, and GitHub login method statuses.
        /// </returns>
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

            // Load only the fields needed to calculate provider status.
            var linkedProviders = await _dbContext.UserAuthProviders
                .Where(x => x.UserId == userId)
                .Select(x => new
                {
                    x.Provider,
                    x.EmailVerifiedAt
                })
                .ToListAsync(cancellationToken);

            // A local login method is usable only after its email is verified.
            var hasVerifiedLocal = linkedProviders.Any(x =>
                x.Provider == AuthProviders.Local &&
                x.EmailVerifiedAt != null);

            // A pending local login exists when local login was linked but not verified yet.
            var hasPendingLocal = linkedProviders.Any(x =>
                x.Provider == AuthProviders.Local &&
                x.EmailVerifiedAt == null);

            var hasGoogle = linkedProviders.Any(x => x.Provider == AuthProviders.Google);
            var hasGitHub = linkedProviders.Any(x => x.Provider == AuthProviders.GitHub);

            // Count only usable login methods.
            var usableProviderCount = 0;
            usableProviderCount += hasVerifiedLocal ? 1 : 0;
            usableProviderCount += hasGoogle ? 1 : 0;
            usableProviderCount += hasGitHub ? 1 : 0;

            return new List<LoginMethodStatusDto>
            {
                new LoginMethodStatusDto
                {
                    Provider = AuthProviders.Local,
                    DisplayName = "Local",
                    IsLinked = hasVerifiedLocal,
                    CanUnlink = hasVerifiedLocal && usableProviderCount > 1,
                    RequiresVerification = hasPendingLocal
                },
                new LoginMethodStatusDto
                {
                    Provider = AuthProviders.Google,
                    DisplayName = "Google",
                    IsLinked = hasGoogle,
                    CanUnlink = hasGoogle && usableProviderCount > 1
                },
                new LoginMethodStatusDto
                {
                    Provider = AuthProviders.GitHub,
                    DisplayName = "GitHub",
                    IsLinked = hasGitHub,
                    CanUnlink = hasGitHub && usableProviderCount > 1
                }
            };
        }

        /// <summary>
        /// Links local email/password login to an existing user account.
        /// </summary>
        /// <remarks>
        /// This method creates a local auth provider with an unverified email.
        /// The user must verify the email before the local login method becomes usable.
        /// </remarks>
        public async Task<LinkLocalLoginResponseDto> LinkLocalLoginAsync(
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
                // If local login already exists but is not verified, keep the same flow pending.
                if (existingLocalProvider.EmailVerifiedAt == null)
                {
                    return CreatePendingLocalLinkResponse(
                        existingLocalProvider.Email ?? existingLocalProvider.ProviderUserId);
                }

                throw new ConflictException("This account already has a local login method");
            }

            // Prevent the same local email from being used by another local account.
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

            // Send verification code after the local provider is created.
            await _emailVerificationService.SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                email,
                EmailVerificationPurposes.LinkLocal,
                cancellationToken);

            return CreatePendingLocalLinkResponse(email);
        }

        /// <summary>
        /// Creates a response for a local login method that still requires email verification.
        /// </summary>
        private static LinkLocalLoginResponseDto CreatePendingLocalLinkResponse(string? email)
        {
            return new LinkLocalLoginResponseDto
            {
                Message = "Password login is pending verification. Please verify your email to finish linking it.",
                Email = email ?? string.Empty,
                RequiresEmailVerification = true,
                VerificationPurpose = EmailVerificationPurposes.LinkLocal,
                CanResendVerification = true
            };
        }

        /// <summary>
        /// Changes the password of the current user's local login method.
        /// </summary>
        /// <remarks>
        /// The local email must already be verified before password changes are allowed.
        /// </remarks>
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

            // Verify the current password before replacing the stored password hash.
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

        /// <summary>
        /// Links a GitHub OAuth account to an existing user account.
        /// </summary>
        public async Task LinkGitHubAsync(
            Guid userId,
            ClaimsPrincipal githubUser,
            string? githubAccessToken,
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
                accessToken: githubAccessToken,
                emailVerifiedAt: githubEmail == null ? null : DateTime.UtcNow,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Links a Google OAuth account to an existing user account.
        /// </summary>
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
                accessToken: null,
                emailVerifiedAt: DateTime.UtcNow,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Unlinks an authentication provider from a user account.
        /// </summary>
        /// <remarks>
        /// This method prevents users from removing their only login method.
        /// </remarks>
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

        /// <summary>
        /// Links or updates an external OAuth provider for an existing user account.
        /// </summary>
        /// <remarks>
        /// This shared helper is used by both Google and GitHub linking flows.
        ///
        /// It protects against:
        /// - Linking a provider to a missing user.
        /// - Replacing an already linked provider with a different external account.
        /// - Linking the same external provider account to multiple users.
        /// </remarks>
        private async Task LinkExternalProviderAsync(
            Guid userId,
            string provider,
            string providerUserId,
            string? email,
            string? providerUsername,
            string? accessToken,
            DateTime? emailVerifiedAt,
            CancellationToken cancellationToken = default)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId, cancellationToken);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var currentUserProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(
                    x => x.UserId == userId &&
                         x.Provider == provider,
                    cancellationToken);

            var normalizedEmail = NormalizeNullable(email);
            var normalizedProviderUsername = NormalizeNullable(providerUsername);
            var normalizedAccessToken = NormalizeNullable(accessToken);

            if (currentUserProvider != null)
            {
                if (currentUserProvider.ProviderUserId != providerUserId)
                {
                    throw new ConflictException($"This account already has a different {provider} account linked. Disconnect it before linking another one.");
                }

                // Same provider account is already linked, so refresh provider metadata when available.
                if (!string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    currentUserProvider.Email = normalizedEmail;
                }

                if (!string.IsNullOrWhiteSpace(normalizedProviderUsername))
                {
                    currentUserProvider.ProviderUsername = normalizedProviderUsername;
                }

                if (!string.IsNullOrWhiteSpace(normalizedAccessToken))
                {
                    currentUserProvider.AccessToken = normalizedAccessToken;
                }

                if (emailVerifiedAt.HasValue)
                {
                    currentUserProvider.EmailVerifiedAt = emailVerifiedAt;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
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
                ProviderUsername = normalizedProviderUsername,
                Email = normalizedEmail,
                AccessToken = normalizedAccessToken,
                PendingEmail = null,
                PasswordHash = null,
                EmailVerifiedAt = emailVerifiedAt,
                CreatedAt = now
            };

            _dbContext.UserAuthProviders.Add(authProvider);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Normalizes and validates an email address.
        /// </summary>
        private static string NormalizeEmailOrThrow(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email was not provided");
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!IsValidEmailFormat(normalizedEmail))
            {
                throw new InvalidOperationException("Invalid email format");
            }

            return normalizedEmail;
        }

        /// <summary>
        /// Checks whether an email address has a valid format.
        /// </summary>
        private static bool IsValidEmailFormat(string email)
        {
            return new EmailAddressAttribute().IsValid(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Trims an optional string value and converts empty values to null.
        /// </summary>
        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
