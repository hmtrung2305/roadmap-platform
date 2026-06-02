using System.Security.Claims;
using RoadmapPlatform.Application.DTOs.Auth;

namespace RoadmapPlatform.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<RegistrationResponseDto> RegisterAsync(RegisterRequestDto request);

    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    Task<LoginResponseDto> VerifyRegistrationEmailAsync(
        VerifyRegistrationEmailRequestDto request);

    Task ResendRegistrationVerificationAsync(
        ResendRegistrationVerificationRequestDto request);

    Task<LoginResponseDto> LoginWithGoogleAsync(ClaimsPrincipal googleUser);

    Task<LoginResponseDto> LoginWithGitHubAsync(ClaimsPrincipal githubUser);
}
