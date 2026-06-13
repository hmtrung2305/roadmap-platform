using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpsertQuizQuestionRequestDto
{
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public string QuestionType { get; set; } = LearningModuleQuestionTypeValues.SingleChoice;

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public int Points { get; set; } = 1;

    [MinLength(2)]
    public List<UpsertQuizOptionRequestDto> Options { get; set; } = [];
}

public sealed class UpsertQuizOptionRequestDto
{
    public Guid? SkillModuleQuizOptionId { get; set; }

    [Required]
    public string OptionText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }
}
