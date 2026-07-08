using System.ComponentModel.DataAnnotations;
using RoadmapPlatform.Application.Interfaces.Security;

namespace RoadmapPlatform.Application.DTOs.Auth;

/// <summary>
/// Represents the request payload used to log in with an email/username and password.
/// </summary>
/// <remarks>
/// This DTO is received by the login endpoint.
///
/// It implements ICaptchaProtectedRequest so the API captcha filter can read
/// the captcha token from the request when captcha validation is required.
/// </remarks>
public class LoginRequestDto : ICaptchaProtectedRequest
{
    /// <summary>
    /// Gets or sets the email address or username used for login.
    /// </summary>
    /// <remarks>
    /// This field is required.
    /// The authentication service decides whether the value matches an email
    /// or a username.
    /// </remarks>
    [Required(ErrorMessage = "Email or username is required")]
    public string? EmailOrUsername { get; set; }

    /// <summary>
    /// Gets or sets the user's plain-text password from the login request.
    /// </summary>
    /// <remarks>
    /// This field is required.
    /// It should only be used for verification during login and must never be logged.
    /// </remarks>
    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the captcha token submitted by the frontend.
    /// </summary>
    /// <remarks>
    /// This value is used by the captcha validation filter before the login logic runs.
    /// It can be null when captcha is disabled or not required for the current endpoint.
    /// </remarks>
    public string? CaptchaToken { get; set; }
}