namespace RoadmapPlatform.Application.Models.GitHub;

public sealed class GitHubAccessTokenContext
{
    public required string Username { get; init; }

    public required string AccessToken { get; init; }
}
