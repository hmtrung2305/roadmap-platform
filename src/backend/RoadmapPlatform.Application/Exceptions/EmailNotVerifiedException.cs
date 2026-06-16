namespace RoadmapPlatform.Application.Exceptions;

public class EmailNotVerifiedException : Exception
{
    public string Email { get; }

    public string VerificationPurpose { get; }

    public bool CanResendVerification { get; }

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
