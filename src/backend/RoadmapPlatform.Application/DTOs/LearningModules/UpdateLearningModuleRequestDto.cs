using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLearningModuleRequestDto
{
    public Guid? SkillId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    [MaxLength(30)]
    public string? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }

    public Dictionary<string, object?>? Metadata { get; set; }
}
