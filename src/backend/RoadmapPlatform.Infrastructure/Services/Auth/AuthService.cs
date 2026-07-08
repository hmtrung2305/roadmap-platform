using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.Auth;

/// <summary>
/// Implements authentication use cases for local registration, local login,
/// email verification, and external OAuth login.
/// </summary>
/// <remarks>
/// This service contains the main authentication business logic.
/// It works with the database, password hasher, JWT token service,
/// OAuth login service, and email verification service.
/// </remarks>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOAuthLoginService _oauthLoginService;
    private readonly IEmailVerificationService _emailVerificationService;

    /// <summary>
    /// Creates a new authentication service instance.
    /// </summary>
    public AuthService(
        ApplicationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        IOAuthLoginService oauthLoginService,
        IEmailVerificationService emailVerificationService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _oauthLoginService = oauthLoginService;
        _emailVerificationService = emailVerificationService;
    }

    /// <summary>
    /// Starts local account registration and sends an email verification code.
    /// </summary>
    /// <param name="request">
    /// The registration request containing username, email, password, and optional captcha token.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A registration response telling the client that email verification is required.
    /// </returns>
    /// <remarks>
    /// This method does not create the final user immediately.
    /// It creates or updates a pending local registration first.
    /// The real user account is created after email verification succeeds.
    /// </remarks>
    public async Task<RegistrationResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        // Normalize user input before checking uniqueness.
        var username = NormalizeUsernameOrThrow(request.Username);
        var normalizedUsername = username.ToLowerInvariant();
        var email = NormalizeEmailOrThrow(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Password was not provided");
        }

        // Check whether this email already has an unused pending registration.
        var existingPendingRegistration = await _dbContext.PendingLocalRegistrations
            .FirstOrDefaultAsync(
                x => x.Email == email &&
                     x.UsedAt == null,
                cancellationToken);

        // If a valid pending registration already exists, ask the user to verify it.
        if (existingPendingRegistration != null &&
            existingPendingRegistration.ExpiresAt > DateTime.UtcNow)
        {
            return CreatePendingRegistrationResponse(email);
        }

        // Check whether the email is already linked to an existing local auth provider.
        var existingLocalProvider = await _dbContext.UserAuthProviders
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Provider == AuthProviders.Local &&
                     x.ProviderUserId == email,
                cancellationToken);

        if (existingLocalProvider != null)
        {
            // If the account exists but is still not verified, continue the verification flow.
            if (existingLocalProvider.EmailVerifiedAt == null ||
                existingLocalProvider.User?.Status == UserStatuses.PendingVerification)
            {
                return CreatePendingRegistrationResponse(email);
            }

            throw new ConflictException("Email is already registered");
        }

        // Check username uniqueness.
        var usernameExists = await _dbContext.Users
            .AnyAsync(x => x.UsernameNormalized == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            throw new ConflictException("Username is already taken");
        }

        var now = DateTime.UtcNow;

        if (existingPendingRegistration != null)
        {
            // Reuse the existing expired pending registration and refresh its data.
            existingPendingRegistration.Username = username;
            existingPendingRegistration.UsernameNormalized = normalizedUsername;
            existingPendingRegistration.PasswordHash = _passwordHasher.HashPassword(
                CreatePasswordHashUser(username, normalizedUsername),
                request.Password);
            existingPendingRegistration.ExpiresAt = now.AddDays(7);
            existingPendingRegistration.UpdatedAt = now;
        }
        else
        {
            // Create a new pending registration record.
            var pendingRegistration = new PendingLocalRegistration
            {
                PendingLocalRegistrationId = Guid.NewGuid(),
                Username = username,
                UsernameNormalized = normalizedUsername,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(
                    CreatePasswordHashUser(username, normalizedUsername),
                    request.Password),
                ExpiresAt = now.AddDays(7),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.PendingLocalRegistrations.Add(pendingRegistration);
            existingPendingRegistration = pendingRegistration;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send the email verification code for this pending registration.
        await _emailVerificationService.SendPendingRegistrationVerificationCodeAsync(
            existingPendingRegistration.PendingLocalRegistrationId,
            email,
            cancellationToken);

        return new RegistrationResponseDto
        {
            Message = "Registration started. Please verify your email.",
            Email = email,
            RequiresEmailVerification = true,
            VerificationPurpose = EmailVerificationPurposes.Register,
            CanResendVerification = true
        };
    }

    /// <summary>
    /// Authenticates a local user using email/username and password.
    /// </summary>
    /// <param name="request">
    /// The login request containing email or username and password.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing an access token and user data.
    /// </returns>
    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
        {
            throw new InvalidOperationException("Email or username was not provided");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Password was not provided");
        }

        var emailOrUsername = request.EmailOrUsername.Trim();

        UserAuthProvider? localProvider;

        if (emailOrUsername.Contains('@'))
        {
            // Login by email.
            var email = NormalizeEmailOrThrow(emailOrUsername);

            localProvider = await _dbContext.UserAuthProviders
                .Include(x => x.User)
                .ThenInclude(u => u!.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(
                    x => x.Provider == AuthProviders.Local &&
                         x.ProviderUserId == email,
                    cancellationToken);
        }
        else
        {
            // Login by username.
            var usernameNormalized = NormalizeUsernameOrThrow(emailOrUsername).ToLowerInvariant();

            localProvider = await _dbContext.UserAuthProviders
                .Include(x => x.User)
                .ThenInclude(u => u!.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(
                    x => x.Provider == AuthProviders.Local &&
                         x.User != null &&
                         x.User.UsernameNormalized == usernameNormalized,
                    cancellationToken);
        }

        if (localProvider == null ||
            localProvider.User == null ||
            string.IsNullOrWhiteSpace(localProvider.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Verify the submitted password against the stored password hash.
        var verificationResult = _passwordHasher.VerifyHashedPassword(
            localProvider.User,
            localProvider.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Local users must verify their email before logging in.
        if (localProvider.EmailVerifiedAt == null)
        {
            throw new EmailNotVerifiedException(localProvider.Email ?? localProvider.ProviderUserId);
        }

        ValidateAccountStatus(localProvider.User.Status);

        // Make sure old accounts without roles still get the default Learner role.
        await EnsureDefaultLearnerRoleIfNoRolesAsync(localProvider.User, cancellationToken);

        var authenticatedUser = new AuthenticatedUserDto
        {
            UserId = localProvider.User.UserId,
            Username = localProvider.User.Username,
            Email = localProvider.Email,
            Status = localProvider.User.Status,
            Roles = localProvider.User.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleName)
                .ToList()
        };

        return CreateLoginResponse(authenticatedUser);
    }

    /// <summary>
    /// Verifies a pending registration email and creates the final local user account.
    /// </summary>
    /// <param name="request">
    /// The verification request containing email and OTP.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response for the newly verified user.
    /// </returns>
    /// <remarks>
    /// This method converts a pending registration into:
    /// - User.
    /// - UserProfile.
    /// - UserAuthProvider.
    /// - Default Learner UserRole.
    /// </remarks>
    public async Task<LoginResponseDto> VerifyRegistrationEmailAsync(
        VerifyRegistrationEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        var email = NormalizeEmailOrThrow(request.Email);

        if (string.IsNullOrWhiteSpace(request.Otp))
        {
            throw new InvalidOperationException("OTP was not provided");
        }

        // Verify the OTP and get the pending registration data.
        var verificationResult = await _emailVerificationService
            .VerifyRegistrationEmailAsync(email, request.Otp, cancellationToken);

        // Re-check username uniqueness before creating the final user.
        var usernameExists = await _dbContext.Users
            .AnyAsync(
                x => x.UsernameNormalized == verificationResult.UsernameNormalized,
                cancellationToken);

        if (usernameExists)
        {
            throw new ConflictException("Username is already taken. Please restart registration with another username.");
        }

        // Re-check email uniqueness before creating the local auth provider.
        var emailExists = await _dbContext.UserAuthProviders
            .AnyAsync(
                x => x.Provider == AuthProviders.Local &&
                     x.ProviderUserId == verificationResult.Email,
                cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("Email is already registered");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = verificationResult.Username,
            UsernameNormalized = verificationResult.UsernameNormalized,
            Status = UserStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        var profile = new UserProfile
        {
            User = user,
            IsPublic = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var localProvider = new UserAuthProvider
        {
            User = user,
            Provider = AuthProviders.Local,
            ProviderUserId = verificationResult.Email,
            Email = verificationResult.Email,
            PendingEmail = null,
            PasswordHash = verificationResult.PasswordHash,
            EmailVerifiedAt = now,
            CreatedAt = now
        };

        _dbContext.Users.Add(user);
        _dbContext.UserProfiles.Add(profile);
        _dbContext.UserAuthProviders.Add(localProvider);

        var learnerRole = await GetRequiredLearnerRoleAsync(cancellationToken);

        var learnerUserRole = new UserRole
        {
            User = user,
            UserId = user.UserId,
            Role = learnerRole,
            RoleId = learnerRole.RoleId
        };

        _dbContext.UserRoles.Add(learnerUserRole);
        user.UserRoles.Add(learnerUserRole);

        // Mark the pending registration as used after successful verification.
        var pendingRegistration = await _dbContext.PendingLocalRegistrations
            .FirstOrDefaultAsync(
                x => x.PendingLocalRegistrationId == verificationResult.PendingLocalRegistrationId,
                cancellationToken);

        if (pendingRegistration != null)
        {
            pendingRegistration.UsedAt = now;
            pendingRegistration.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var authenticatedUser = new AuthenticatedUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = verificationResult.Email,
            Status = user.Status,
            Roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleName)
                .ToList()
        };

        return CreateLoginResponse(authenticatedUser);
    }

    /// <summary>
    /// Resends the registration verification code for a pending local registration.
    /// </summary>
    public async Task ResendRegistrationVerificationAsync(
        ResendRegistrationVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        var email = NormalizeEmailOrThrow(request.Email);

        // Delegate verification resend logic to the email verification service.
        await _emailVerificationService.ResendRegistrationVerificationAsync(
            email,
            cancellationToken);
    }

    /// <summary>
    /// Logs in or creates a user from a GitHub OAuth identity.
    /// </summary>
    /// <param name="githubUser">
    /// The GitHub claims principal returned by the OAuth middleware.
    /// </param>
    /// <param name="githubAccessToken">
    /// The GitHub OAuth access token, when available.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing an access token and user data.
    /// </returns>
    public async Task<LoginResponseDto> LoginWithGitHubAsync(
        ClaimsPrincipal githubUser,
        string? githubAccessToken,
        CancellationToken cancellationToken = default)
    {
        var githubUserId = githubUser.FindFirstValue(ClaimTypes.NameIdentifier);
        var githubEmail = githubUser.FindFirstValue(ClaimTypes.Email);
        var githubUsername = githubUser.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(githubUserId))
        {
            throw new InvalidOperationException("GitHub ID was not provided.");
        }

        if (string.IsNullOrWhiteSpace(githubUsername))
        {
            throw new InvalidOperationException("GitHub username was not provided.");
        }

        var externalLogin = new OAuthUserInfoDto
        {
            Provider = AuthProviders.GitHub,
            ProviderUserId = githubUserId,
            ProviderUsername = githubUsername,
            Email = githubEmail,
            AccessToken = githubAccessToken
        };

        // Delegate OAuth account linking/creation to the OAuth login service.
        var user = await _oauthLoginService.LoginOrCreateUserAsync(externalLogin);

        ValidateAccountStatus(user.Status);

        return CreateLoginResponse(user);
    }

    /// <summary>
    /// Logs in or creates a user from a Google OAuth identity.
    /// </summary>
    /// <param name="googleUser">
    /// The Google claims principal returned by the OAuth middleware.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing an access token and user data.
    /// </returns>
    public async Task<LoginResponseDto> LoginWithGoogleAsync(
        ClaimsPrincipal googleUser,
        CancellationToken cancellationToken = default)
    {
        var googleUserId = googleUser.FindFirstValue(ClaimTypes.NameIdentifier);
        var googleEmail = googleUser.FindFirstValue(ClaimTypes.Email);
        var googleName = googleUser.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            throw new InvalidOperationException("Google ID was not provided");
        }

        if (string.IsNullOrWhiteSpace(googleEmail))
        {
            throw new InvalidOperationException("Google email was not provided");
        }

        var externalLogin = new OAuthUserInfoDto
        {
            Provider = AuthProviders.Google,
            ProviderUserId = googleUserId,
            DisplayName = googleName,
            Email = googleEmail
        };

        // Delegate OAuth account linking/creation to the OAuth login service.
        var user = await _oauthLoginService.LoginOrCreateUserAsync(externalLogin);

        ValidateAccountStatus(user.Status);

        return CreateLoginResponse(user);
    }

    /// <summary>
    /// Creates a temporary user object used only for password hashing.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core PasswordHasher requires a user instance.
    /// During pending registration, the final user does not exist yet,
    /// so this method creates a temporary user object for hashing only.
    /// </remarks>
    private static User CreatePasswordHashUser(
        string username,
        string usernameNormalized)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            UsernameNormalized = usernameNormalized,
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a response for an email that already has a pending registration.
    /// </summary>
    private static RegistrationResponseDto CreatePendingRegistrationResponse(string email)
    {
        return new RegistrationResponseDto
        {
            Message = "This email already has a pending registration. Verify your email to continue with the original account details.",
            Email = email,
            RequiresEmailVerification = true,
            VerificationPurpose = EmailVerificationPurposes.Register,
            CanResendVerification = true
        };
    }

    /// <summary>
    /// Ensures that a user has the default Learner role when no roles are assigned.
    /// </summary>
    /// <remarks>
    /// This is mainly a safety repair for old or incomplete accounts.
    /// New verified local accounts are already assigned the Learner role
    /// during registration verification.
    /// </remarks>
    private async Task EnsureDefaultLearnerRoleIfNoRolesAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (user.UserRoles.Any())
        {
            return;
        }

        var learnerRole = await GetRequiredLearnerRoleAsync(cancellationToken);

        var userRole = new UserRole
        {
            User = user,
            UserId = user.UserId,
            Role = learnerRole,
            RoleId = learnerRole.RoleId
        };

        _dbContext.UserRoles.Add(userRole);
        user.UserRoles.Add(userRole);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the default Learner role from the database.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Learner role is missing from the database.
    /// </exception>
    private async Task<Role> GetRequiredLearnerRoleAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.RoleName == RoleNames.Learner, cancellationToken)
            ?? throw new InvalidOperationException(
                "Default learner role was not found. Run the RBAC role-permission seed before creating or logging in learner accounts.");
    }

    /// <summary>
    /// Creates a login response for an authenticated user.
    /// </summary>
    /// <remarks>
    /// This method generates the application JWT access token and maps user data
    /// into the public login response DTO.
    /// </remarks>
    private LoginResponseDto CreateLoginResponse(AuthenticatedUserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            throw new InvalidOperationException("Username was not provided");
        }

        var accessToken = _jwtTokenService.GenerateToken(
            user.UserId,
            user.Username,
            user.Roles);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            User = new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Status = user.Status
            }
        };
    }

    /// <summary>
    /// Validates whether the account status allows login.
    /// </summary>
    /// <exception cref="UnauthorizedException">
    /// Thrown when the account has been deleted.
    /// </exception>
    /// <exception cref="ForbiddenException">
    /// Thrown when the account has been suspended.
    /// </exception>
    private static void ValidateAccountStatus(string? status)
    {
        if (status == UserStatuses.Deleted)
        {
            throw new UnauthorizedException("This account has been deleted");
        }

        if (status == UserStatuses.Suspended)
        {
            throw new ForbiddenException("This account has been suspended");
        }
    }

    /// <summary>
    /// Normalizes and validates an email address.
    /// </summary>
    /// <param name="email">
    /// The raw email input.
    /// </param>
    /// <returns>
    /// The normalized lowercase email.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the email is missing or invalid.
    /// </exception>
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
    /// Normalizes and validates a username.
    /// </summary>
    /// <param name="username">
    /// The raw username input.
    /// </param>
    /// <returns>
    /// The trimmed username.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the username is missing.
    /// </exception>
    private static string NormalizeUsernameOrThrow(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Username was not provided");
        }

        return username.Trim();
    }
}
