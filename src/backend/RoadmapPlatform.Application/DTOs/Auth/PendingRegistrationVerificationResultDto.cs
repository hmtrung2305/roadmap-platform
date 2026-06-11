namespace RoadmapPlatform.Application.DTOs.Auth;

public class PendingRegistrationVerificationResultDto
{
    public Guid PendingLocalRegistrationId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string UsernameNormalized { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
