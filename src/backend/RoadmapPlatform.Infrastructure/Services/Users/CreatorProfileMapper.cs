using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users;

internal static class CreatorProfileMapper
{
    public static CreatorProfileDto? Map(User? user)
    {
        var profile = user?.UserProfile;

        if (user == null || user.DeletedAt.HasValue || profile?.IsPublic != true)
        {
            return null;
        }

        return new CreatorProfileDto
        {
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? user.Username
                : profile.DisplayName.Trim(),
            AvatarUrl = TrimOrNull(profile.AvatarUrl),
            Headline = TrimOrNull(profile.Headline),
            Bio = TrimOrNull(profile.Bio),
            GithubUrl = TrimOrNull(profile.GithubUrl),
            LinkedinUrl = TrimOrNull(profile.LinkedinUrl),
            PersonalWebsiteUrl = TrimOrNull(profile.PersonalWebsiteUrl)
        };
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
