namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModulePreviewDto
{
    public Guid SkillModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DifficultyLevel { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public IReadOnlyList<LearningModuleLessonPreviewItemDto> Lessons { get; set; } = [];
    public LearningModuleQuizPreviewDto? Quiz { get; set; }
}

public sealed class LearningModuleLessonPreviewItemDto
{
    public Guid SkillModuleLessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int OrderIndex { get; set; }
    public decimal? EstimatedHours { get; set; }
}

public sealed class LearningModuleQuizPreviewDto
{
    public Guid SkillModuleQuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
    public decimal PassingScorePercent { get; set; }
    public int? MaxAttempts { get; set; }
}
