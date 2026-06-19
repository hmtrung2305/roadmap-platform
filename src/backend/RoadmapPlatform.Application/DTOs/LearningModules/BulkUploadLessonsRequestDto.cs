using RoadmapPlatform.Application.Constants;
using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class BulkUploadLessonsRequestDto
{
    [MinLength(1)]
    [MaxLength(LearningModuleAuthoringLimits.BulkUploadMaxLessonCount)]
    public List<BulkUploadLessonItemDto> Lessons { get; set; } = [];
}

public sealed class BulkUploadLessonItemDto
{
    [Required]
    [MaxLength(80)]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(LearningModuleAuthoringLimits.LessonFileNameMaxLength)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(LearningModuleAuthoringLimits.LessonTitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(LearningModuleAuthoringLimits.LessonSlugMaxLength)]
    public string? Slug { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.LessonSummaryMaxLength)]
    public string? Summary { get; set; }

    public int OrderIndex { get; set; }

    public decimal? EstimatedHours { get; set; }
}
