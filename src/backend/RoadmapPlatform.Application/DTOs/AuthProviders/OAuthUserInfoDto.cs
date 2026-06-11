namespace RoadmapPlatform.Application.DTOs.AuthProviders;

public class OAuthUserInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? ProviderUsername { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AccessToken { get; set; }
}