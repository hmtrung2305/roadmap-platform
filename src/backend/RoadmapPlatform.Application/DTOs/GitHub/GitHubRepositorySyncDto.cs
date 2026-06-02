namespace RoadmapPlatform.Application.DTOs.GitHub
{
    public class GitHubRepositorySyncDto
    {
        public long GithubRepoId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string HtmlUrl { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? PrimaryLanguage { get; set; }

        public int Stars { get; set; }

        public int Forks { get; set; }

        public bool IsPrivate { get; set; }

        public DateTime? GithubCreatedAt { get; set; }

        public DateTime? GithubUpdatedAt { get; set; }
    }
}
