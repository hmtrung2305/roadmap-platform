using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLearningModuleLessonRequestDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }

    public string? Summary { get; set; }

    public decimal? EstimatedHours { get; set; }
}
