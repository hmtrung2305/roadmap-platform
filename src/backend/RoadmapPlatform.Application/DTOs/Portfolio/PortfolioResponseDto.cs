using RoadmapPlatform.Application.DTOs.GitHub;

namespace RoadmapPlatform.Application.DTOs.Portfolio
{
    public class PortfolioResponseDto
    {
        public string? DisplayName { get; set; }
        public string? Headline { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? CareerGoal { get; set; }
        public string? CurrentRole { get; set; }
        public string? PublicEmail { get; set; }
        public string? GithubUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? PersonalWebsiteUrl { get; set; }
        public List<GitHubRepositoryResponseDto> Repositories { get; set; } = new();
    }
}