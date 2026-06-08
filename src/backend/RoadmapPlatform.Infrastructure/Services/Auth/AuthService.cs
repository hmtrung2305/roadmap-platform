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

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOAuthLoginService _oauthLoginService;
    private readonly IEmailVerificationService _emailVerificationService;

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

    public async Task<RegistrationResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        var username = NormalizeUsernameOrThrow(request.Username);
        var normalizedUsername = username.ToLowerInvariant();
        var email = NormalizeEmailOrThrow(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Password was not provided");
        }

        var existingLocalProvider = await _dbContext.UserAuthProviders
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Provider == AuthProviders.Local &&
                     x.ProviderUserId == email,
                cancellationToken);

        if (existingLocalProvider != null)
        {
            if (existingLocalProvider.EmailVerifiedAt == null ||
                existingLocalProvider.User?.Status == UserStatuses.PendingVerification)
            {
                return CreatePendingRegistrationResponse(email);
            }

            throw new ConflictException("Email is already registered");
        }

        var usernameExists = await _dbContext.Users
            .AnyAsync(x => x.UsernameNormalized == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            throw new ConflictException("Username is already taken");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            UsernameNormalized = normalizedUsername,
            Status = UserStatuses.PendingVerification,
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
            UserId = user.UserId,
            Provider = AuthProviders.Local,
            ProviderUserId = email,
            Email = email,
            PendingEmail = null,
            PasswordHash = _passwordHasher.HashPassword(user, request.Password),
            EmailVerifiedAt = null,
            CreatedAt = now
        };

        _dbContext.Users.Add(user);
        _dbContext.UserProfiles.Add(profile);
        _dbContext.UserAuthProviders.Add(localProvider);

        var learnerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.RoleName == RoleNames.Learner, cancellationToken);

        if (learnerRole != null)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = learnerRole.RoleId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailVerificationService.SendVerificationCodeAsync(
            user.UserId,
            AuthProviders.Local,
            email,
            EmailVerificationPurposes.Register,
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

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            localProvider.User,
            localProvider.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        if (localProvider.EmailVerifiedAt == null)
        {
            throw new EmailNotVerifiedException(localProvider.Email ?? localProvider.ProviderUserId);
        }

        ValidateAccountStatus(localProvider.User.Status);

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

        var verificationResult = await _emailVerificationService
            .VerifyRegistrationEmailAsync(email, request.Otp, cancellationToken);

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.UserId == verificationResult.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User was not found");
        }

        ValidateAccountStatus(user.Status);

        if (user.Status == UserStatuses.PendingVerification)
        {
            user.Status = UserStatuses.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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

    public async Task ResendRegistrationVerificationAsync(
        ResendRegistrationVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        var email = NormalizeEmailOrThrow(request.Email);

        await _emailVerificationService.ResendRegistrationVerificationAsync(
            email,
            cancellationToken);
    }

    public async Task<LoginResponseDto> LoginWithGitHubAsync(
        ClaimsPrincipal githubUser,
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
            Email = githubEmail
        };

        var user = await _oauthLoginService.LoginOrCreateUserAsync(externalLogin);

        ValidateAccountStatus(user.Status);

        return CreateLoginResponse(user);
    }

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

        var user = await _oauthLoginService.LoginOrCreateUserAsync(externalLogin);

        ValidateAccountStatus(user.Status);

        return CreateLoginResponse(user);
    }

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

    private static bool IsValidEmailFormat(string email)
    {
        return new EmailAddressAttribute().IsValid(email) &&
               Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private static string NormalizeUsernameOrThrow(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Username was not provided");
        }

        return username.Trim();
    }
}
