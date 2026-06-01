using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOAuthLoginService _oauthLoginService;

    public AuthService(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOAuthLoginService oauthLoginService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _oauthLoginService = oauthLoginService;
    }

    public async Task<LoginResponseDto> LoginWithGitHubAsync(ClaimsPrincipal githubUser)
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

    public async Task<LoginResponseDto> LoginWithGoogleAsync(ClaimsPrincipal googleUser)
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

    private LoginResponseDto CreateLoginResponse(AuthenticatedUserDto user)
    {
        string username = user.Username;

        var accessToken = _jwtTokenService.GenerateToken(user.UserId, user.Username ?? string.Empty);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            User = new UserResponseDto
            {
                UserId = user.UserId,
                Username = username,
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
}
