namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleRagSourceDto
{
    public Guid SkillModuleChunkId { get; set; }

    public Guid SkillModuleLessonId { get; set; }

    public string LessonTitle { get; set; } = string.Empty;

    public string? Heading { get; set; }

    public string ContentPreview { get; set; } = string.Empty;

    public double? SimilarityScore { get; set; }
}
