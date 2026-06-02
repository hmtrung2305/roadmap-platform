namespace RoadmapPlatform.Application.DTOs.AuthProviders
{
    public class LoginMethodStatusDto
    {
        public string Provider { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsLinked { get; set; }

        public bool CanUnlink { get; set; }

        public bool RequiresVerification { get; set; }
    }
}