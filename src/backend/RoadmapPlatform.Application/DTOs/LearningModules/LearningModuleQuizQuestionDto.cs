namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleQuizQuestionDto
{
    public Guid SkillModuleQuizQuestionId { get; set; }
    public Guid SkillModuleQuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = LearningModuleQuestionTypeValues.SingleChoice;
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public IReadOnlyList<LearningModuleQuizOptionDto> Options { get; set; } = [];
}
