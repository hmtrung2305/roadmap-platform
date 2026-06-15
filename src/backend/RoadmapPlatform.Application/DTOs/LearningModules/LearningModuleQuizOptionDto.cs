namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleQuizOptionDto
{
    public Guid SkillModuleQuizOptionId { get; set; }
    public Guid SkillModuleQuizQuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
}
