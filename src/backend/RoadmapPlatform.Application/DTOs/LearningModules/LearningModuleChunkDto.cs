namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleChunkDto
{
    public Guid SkillModuleChunkId { get; set; }
    public Guid SkillModuleId { get; set; }
    public Guid SkillModuleLessonId { get; set; }
    public int ChunkIndex { get; set; }
    public string? Heading { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TokenCount { get; set; }
    public string? ContentHash { get; set; }
}
