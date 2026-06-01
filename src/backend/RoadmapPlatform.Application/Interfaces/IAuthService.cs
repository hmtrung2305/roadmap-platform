using System.Security.Claims;
using RoadmapPlatform.Application.DTOs.Auth;

namespace RoadmapPlatform.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginWithGoogleAsync(ClaimsPrincipal googleUser);
    Task<LoginResponseDto> LoginWithGitHubAsync(ClaimsPrincipal githubUser);
}
