using RoadmapPlatform.Application.Constants;
using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class CreateLearningModuleRequestDto
{
    [Required]
    public Guid SkillId { get; set; }

    [Required]
    [MaxLength(LearningModuleAuthoringLimits.ModuleTitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(LearningModuleAuthoringLimits.ModuleSlugMaxLength)]
    public string? Slug { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.ModuleDescriptionMaxLength)]
    public string? Description { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.DifficultyLevelMaxLength)]
    public string? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }
}
