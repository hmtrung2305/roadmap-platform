using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class CreateLearningModuleRequestDto
{
    [Required]
    public Guid SkillId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    [MaxLength(30)]
    public string? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }
}
