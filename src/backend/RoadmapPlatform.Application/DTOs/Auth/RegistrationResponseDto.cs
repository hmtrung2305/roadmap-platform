namespace RoadmapPlatform.Application.DTOs.Auth;

public class RegistrationResponseDto
{
    public string Message { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool RequiresEmailVerification { get; set; }

    public string VerificationPurpose { get; set; } = string.Empty;

    public bool CanResendVerification { get; set; }
}
