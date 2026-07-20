namespace RoadmapPlatform.Application.DTOs.GitHub
{
    public class GeneratedRepoInsightDto
    {
        public bool HasSufficientEvidence { get; set; }

        public string? InsufficientReason { get; set; }

        public string? PurposeEvidence { get; set; }

        public string Summary { get; set; } = string.Empty;

        public List<string> TechStack { get; set; } = new();

        public List<string> DetectedSkills { get; set; } = new();

        public string ProjectType { get; set; } = "Other";

        public string AiModel { get; set; } = string.Empty;
    }
}
