using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLessonProgressRequestDto
{
    [Required]
    public string Status { get; set; } = LearningModuleLessonProgressStatusValues.InProgress;
}
