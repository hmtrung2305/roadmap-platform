namespace RoadmapPlatform.Application.DTOs.Auth;

public class EmailVerificationRequiredResponseDto
{
    public string Message { get; set; } = "Email verification is required";

    public string Email { get; set; } = string.Empty;

    public bool RequiresEmailVerification { get; set; } = true;
}