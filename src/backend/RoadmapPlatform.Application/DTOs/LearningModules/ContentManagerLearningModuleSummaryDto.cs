namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ContentManagerLearningModuleSummaryDto
{
    public Guid SkillModuleId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string SkillSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DifficultyLevel { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public int QuestionCount { get; set; }
    public bool HasQuiz { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
