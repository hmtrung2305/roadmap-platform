using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Auth;

/// <summary>
/// Defines the OAuth login use case for external authentication providers.
/// </summary>
/// <remarks>
/// This interface handles the common OAuth login flow after an external provider
/// such as Google or GitHub has authenticated the user.
///
/// The implementation should either:
/// - Find an existing user linked to the external provider account.
/// - Or create a new user and link the external provider account.
///
/// This interface does not start the OAuth redirect flow.
/// The redirect and callback flow are handled by the API authentication middleware
/// and controllers.
/// </remarks>
public interface IOAuthLoginService
{
    /// <summary>
    /// Logs in an existing user or creates a new user from external OAuth user information.
    /// </summary>
    /// <param name="externalLogin">
    /// The normalized external OAuth user information returned by a provider such as Google or GitHub.
    /// </param>
    /// <returns>
    /// The authenticated application user data used to create an application access token.
    /// </returns>
    /// <remarks>
    /// This method should return an AuthenticatedUserDto because the caller still needs
    /// to generate the application's own JWT access token after the OAuth login succeeds.
    /// </remarks>
    Task<AuthenticatedUserDto> LoginOrCreateUserAsync(OAuthUserInfoDto externalLogin);
}
