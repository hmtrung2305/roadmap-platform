namespace RoadmapPlatform.Application.Exceptions;

/// <summary>
/// Represents an application-level error that occurs when a user's email
/// has not been verified yet.
/// </summary>
/// <remarks>
/// This exception should be thrown when an action requires a verified email,
/// but the current user's email verification status is still incomplete.
///
/// The API layer maps this exception to HTTP 403 Forbidden through
/// ApiErrorResponseFactory.
///
/// Unlike a normal ForbiddenException, this exception also carries extra
/// verification-related information so the frontend can guide the user to
/// verify or resend the verification email.
/// </remarks>
public sealed class EmailNotVerifiedException : Exception
{
    /// <summary>
    /// Gets the email address that still requires verification.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the purpose of the email verification flow.
    /// </summary>
    public string VerificationPurpose { get; }

    /// <summary>
    /// Gets a value indicating whether the user is allowed to request
    /// another verification email.
    /// </summary>
    public bool CanResendVerification { get; }

    /// <summary>
    /// Creates a new email-not-verified exception.
    /// </summary>
    /// <param name="email">
    /// The email address that has not been verified.
    /// </param>
    /// <param name="verificationPurpose">
    /// The purpose of the verification flow. The default value is "register".
    /// </param>
    /// <param name="canResendVerification">
    /// Indicates whether the frontend should allow the user to resend
    /// the verification email.
    /// </param>
    public EmailNotVerifiedException(
        string email,
        string verificationPurpose = "register",
        bool canResendVerification = true)
        : base("Email has not been verified")
    {
        Email = email;
        VerificationPurpose = verificationPurpose;
        CanResendVerification = canResendVerification;
    }
}
