using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.DTOs.Auth;

/// <summary>
/// Represents the result returned by the authentication service after a successful login.
/// </summary>
/// <remarks>
/// This DTO contains the generated access token and the authenticated user's public profile.
///
/// In the API controller, the access token is usually written into an HttpOnly cookie
/// instead of being returned directly to the frontend response body.
/// </remarks>
public class LoginResponseDto
{
    /// <summary>
    /// Gets or sets the JWT token access token created for the authenticated user.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets the authenticated user's public response data.
    /// </summary>
    public UserResponseDto User { get; set; } = new();
}