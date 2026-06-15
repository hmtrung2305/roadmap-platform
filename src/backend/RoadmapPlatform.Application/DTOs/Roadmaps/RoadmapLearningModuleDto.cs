namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapLearningModuleDto
{
    public Guid SkillModuleId { get; set; }
    public Guid SkillId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DifficultyLevel { get; set; }
    public decimal? EstimatedHours { get; set; }
    public int LessonCount { get; set; }
    public int QuestionCount { get; set; }
    public string Provider { get; set; } = "Roadmap Platform";
}
