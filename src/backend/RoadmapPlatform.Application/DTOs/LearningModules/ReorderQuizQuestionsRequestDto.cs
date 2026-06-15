using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ReorderQuizQuestionsRequestDto
{
    [MinLength(1)]
    public List<ReorderQuizQuestionItemDto> Questions { get; set; } = [];
}

public sealed class ReorderQuizQuestionItemDto
{
    [Required]
    public Guid SkillModuleQuizQuestionId { get; set; }

    public int OrderIndex { get; set; }
}
