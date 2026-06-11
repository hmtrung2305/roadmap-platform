namespace RoadmapPlatform.Application.DTOs.GitHub
{
    public class GitHubRepositoryResponseDto
    {
        public Guid RepositoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string HtmlUrl { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? PrimaryLanguage { get; set; }

        public int Stars { get; set; }

        public int Forks { get; set; }

        public bool IsSelectedForPortfolio { get; set; }

        public DateTime SyncedAt { get; set; }

        public RepoInsightResponseDto? Insight { get; set; }

    }
}
