using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpsertQuizRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal PassingScorePercent { get; set; } = 70;

    public int? MaxAttempts { get; set; }
}
