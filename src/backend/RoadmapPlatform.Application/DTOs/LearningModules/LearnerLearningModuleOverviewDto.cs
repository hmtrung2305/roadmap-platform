namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearnerLearningModuleOverviewDto
{
    public Guid SkillModuleId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DifficultyLevel { get; set; }
    public decimal? EstimatedHours { get; set; }
    public IReadOnlyList<LearningModuleLessonPreviewItemDto> Lessons { get; set; } = [];
    public LearningModuleQuizPreviewDto? Quiz { get; set; }
    public LearningModuleEnrollmentDto? Enrollment { get; set; }
}
