namespace RoadmapPlatform.Application.DTOs.GitHub
{
    public class RepoInsightResponseDto
    {
        public Guid InsightId { get; set; }

        public Guid RepositoryId { get; set; }

        public string? Summary { get; set; }

        public List<string> TechStack { get; set; } = new();

        public List<string> DetectedSkills { get; set; } = new();

        public string? ProjectType { get; set; }

        public string AnalysisStatus { get; set; } = string.Empty;

        public bool ReadmeTruncated { get; set; }

        public string? AiModel { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime AnalyzedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
