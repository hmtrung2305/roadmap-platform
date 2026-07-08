using System.ComponentModel.DataAnnotations;
using RoadmapPlatform.Application.Interfaces.Security;

namespace RoadmapPlatform.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the request payload used to register a new user account.
    /// </summary>
    /// <remarks>
    /// This DTO is received by the registration endpoint.
    ///
    /// It implements ICaptchaProtectedRequest so the API captcha filter can read
    /// the captcha token when captcha validation is required.
    /// </remarks>
    public class RegisterRequestDto : ICaptchaProtectedRequest
    {
        /// <summary>
        /// Gets or sets the username for the new account.
        /// </summary>
        /// <remarks>
        /// The username is required and must be between 3 and 40 characters.
        ///
        /// Allowed characters:
        /// - Letters.
        /// - Numbers.
        /// - Dot.
        /// - Underscore.
        /// - Hyphen.
        /// </remarks>
        [Required(ErrorMessage = "Username is required")]
        [StringLength(40, MinimumLength = 3,
                ErrorMessage = "Username must be between 3 and 40 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$",
                ErrorMessage = "Username may only contain letters, numbers, ., _, and -")]
        public string? Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address for the new account.
        /// </summary>
        /// <remarks>
        /// The email is required and must match a valid email format.
        ///
        /// This value is also used by the email verification flow after registration.
        /// </remarks>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the plain-text password submitted during registration.
        /// </summary>
        /// <remarks>
        /// The password is required and must contain at least:
        /// - 8 characters.
        /// - One lowercase letter.
        /// - One uppercase letter.
        /// - One number.
        /// - One special character.
        ///
        /// This value should only be used for password hashing and must never be logged.
        /// </remarks>
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8)]
        [RegularExpression(
            "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,}",
            ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, number, and special character.")]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the captcha token submitted by the frontend.
        /// </summary>
        /// <remarks>
        /// This value is used by the captcha validation filter before registration logic runs.
        /// It can be null when captcha is disabled or not required.
        /// </remarks>
        public string? CaptchaToken { get; set; }
    }
}