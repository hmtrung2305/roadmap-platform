namespace RoadmapPlatform.Application.DTOs.Auth
{
    public class EmailVerificationResultDto
    {
        public Guid UserId { get; set; }

        public string Email { get; set; } = string.Empty;
    }
}
