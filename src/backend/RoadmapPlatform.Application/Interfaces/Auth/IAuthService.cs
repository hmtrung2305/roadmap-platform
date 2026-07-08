using System.Security.Claims;
using RoadmapPlatform.Application.DTOs.Auth;

namespace RoadmapPlatform.Application.Interfaces.Auth;

/// <summary>
/// Defines authentication use cases for local and external login flows.
/// </summary>
/// <remarks>
/// This interface belongs to the Application layer and describes what
/// authentication operations the system supports.
///
/// The implementation should handle the actual authentication logic such as:
/// - Registering users.
/// - Validating login credentials.
/// - Verifying registration emails.
/// - Resending verification codes.
/// - Logging in with Google.
/// - Logging in with GitHub.
///
/// Controllers should depend on this interface instead of a concrete
/// authentication service implementation.
/// </remarks>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">
    /// The registration request containing username, email, password, and optional captcha token.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A registration response describing the next step, usually email verification.
    /// </returns>
    Task<RegistrationResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with email/username and password.
    /// </summary>
    /// <param name="request">
    /// The login request containing email or username, password, and optional captcha token.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing the generated access token and authenticated user data.
    /// </returns>
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a registration email and completes the registration login flow.
    /// </summary>
    /// <param name="request">
    /// The email verification request containing the email and verification code.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing the generated access token and verified user data.
    /// </returns>
    Task<LoginResponseDto> VerifyRegistrationEmailAsync(
        VerifyRegistrationEmailRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends a registration email verification code.
    /// </summary>
    /// <param name="request">
    /// The resend verification request containing the target email.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous resend operation.
    /// </returns>
    Task ResendRegistrationVerificationAsync(
        ResendRegistrationVerificationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates or creates a user account using a Google OAuth principal.
    /// </summary>
    /// <param name="googleUser">
    /// The authenticated Google claims principal returned by the OAuth middleware.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing the generated access token and authenticated user data.
    /// </returns>
    Task<LoginResponseDto> LoginWithGoogleAsync(
        ClaimsPrincipal googleUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates or creates a user account using a GitHub OAuth principal.
    /// </summary>
    /// <param name="githubUser">
    /// The authenticated GitHub claims principal returned by the OAuth middleware.
    /// </param>
    /// <param name="githubAccessToken">
    /// The GitHub OAuth access token, when available.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A login response containing the generated access token and authenticated user data.
    /// </returns>
    Task<LoginResponseDto> LoginWithGitHubAsync(
        ClaimsPrincipal githubUser,
        string? githubAccessToken,
        CancellationToken cancellationToken = default);
}
