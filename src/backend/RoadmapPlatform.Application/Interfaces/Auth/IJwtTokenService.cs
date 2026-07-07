namespace RoadmapPlatform.Application.Interfaces.Auth;

/// <summary>
/// Defines a service for generating JWT access tokens.
/// </summary>
/// <remarks>
/// This interface belongs to the Application layer and describes the contract
/// for creating authentication tokens.
///
/// The implementation should handle the actual JWT creation details such as:
/// - Claims.
/// - Signing credentials.
/// - Issuer.
/// - Audience.
/// - Expiration time.
///
/// Controllers and application services should depend on this interface instead
/// of depending directly on a concrete JWT implementation.
/// </remarks>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for an authenticated user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the authenticated user.
    /// </param>
    /// <param name="username">
    /// The username of the authenticated user.
    /// </param>
    /// <param name="roles">
    /// The roles assigned to the authenticated user.
    /// </param>
    /// <returns>
    /// A signed JWT access token as a string.
    /// </returns>
    /// <remarks>
    /// The generated token is later used by the API authentication middleware
    /// to identify the user and authorize protected requests.
    ///
    /// The token should contain enough claims for authentication and basic
    /// role-based authorization, but it should not contain sensitive data.
    /// </remarks>
    string GenerateToken(Guid userId, string username, IEnumerable<string> roles);
}
