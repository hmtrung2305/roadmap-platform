using System.Security.Claims;
using RoadmapPlatform.Application.DTOs.Auth;

namespace RoadmapPlatform.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<RegistrationResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<LoginResponseDto> VerifyRegistrationEmailAsync(
        VerifyRegistrationEmailRequestDto request,
        CancellationToken cancellationToken = default);

    Task ResendRegistrationVerificationAsync(
        ResendRegistrationVerificationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<LoginResponseDto> LoginWithGoogleAsync(
        ClaimsPrincipal googleUser,
        CancellationToken cancellationToken = default);

    Task<LoginResponseDto> LoginWithGitHubAsync(
        ClaimsPrincipal githubUser,
        CancellationToken cancellationToken = default);
}
