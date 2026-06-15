using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ReorderLessonsRequestDto
{
    [MinLength(1)]
    public List<ReorderLessonItemDto> Lessons { get; set; } = [];
}

public sealed class ReorderLessonItemDto
{
    [Required]
    public Guid SkillModuleLessonId { get; set; }

    public int OrderIndex { get; set; }
}
