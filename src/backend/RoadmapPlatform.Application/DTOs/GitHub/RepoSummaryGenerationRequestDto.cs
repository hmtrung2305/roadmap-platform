namespace RoadmapPlatform.Application.DTOs.GitHub
{
    public class RepoSummaryGenerationRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? PrimaryLanguage { get; set; }

        public int Stars { get; set; }

        public int Forks { get; set; }

        public string Readme { get; set; } = string.Empty;
    }
}
