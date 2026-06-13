namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleLessonContentDto
{
    public Guid SkillModuleLessonId { get; set; }
    public Guid SkillModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public int ContentVersion { get; set; }
    public string? ContentHash { get; set; }
}
