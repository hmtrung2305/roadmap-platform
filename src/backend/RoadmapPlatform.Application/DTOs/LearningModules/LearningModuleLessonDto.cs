namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleLessonDto
{
    public Guid SkillModuleLessonId { get; set; }
    public Guid SkillModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int OrderIndex { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string MarkdownFileKey { get; set; } = string.Empty;
    public string? MarkdownFileName { get; set; }
    public string? ContentHash { get; set; }
    public long? ContentSizeBytes { get; set; }
    public int ContentVersion { get; set; }
    public int ChunkCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
