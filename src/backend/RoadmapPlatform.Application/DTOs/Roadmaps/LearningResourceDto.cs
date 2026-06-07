namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class LearningResourceDto
{
    public Guid ResourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? DifficultyLevel { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
}
