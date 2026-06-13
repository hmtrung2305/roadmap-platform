namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleQuizDto
{
    public Guid SkillModuleQuizId { get; set; }
    public Guid SkillModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PassingScorePercent { get; set; }
    public int? MaxAttempts { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<LearningModuleQuizQuestionDto> Questions { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
