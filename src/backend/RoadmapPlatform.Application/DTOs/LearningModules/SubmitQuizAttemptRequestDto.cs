using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class SubmitQuizAttemptRequestDto
{
    [MinLength(1)]
    public List<SubmitQuizAnswerRequestDto> Answers { get; set; } = [];
}

public sealed class SubmitQuizAnswerRequestDto
{
    [Required]
    public Guid SkillModuleQuizQuestionId { get; set; }

    [Required]
    public Guid SelectedOptionId { get; set; }
}
