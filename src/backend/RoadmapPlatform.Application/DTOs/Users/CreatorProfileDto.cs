namespace RoadmapPlatform.Application.DTOs.Users;

public sealed class CreatorProfileDto
{
    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? GithubUrl { get; set; }

    public string? LinkedinUrl { get; set; }

    public string? PersonalWebsiteUrl { get; set; }
}
