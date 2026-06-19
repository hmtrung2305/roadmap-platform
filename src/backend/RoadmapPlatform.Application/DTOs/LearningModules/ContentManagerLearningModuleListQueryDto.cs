using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ContentManagerLearningModuleListQueryDto
{
    [StringLength(30)]
    public string? Status { get; set; }

    [StringLength(150)]
    public string? Search { get; set; }

    [StringLength(30)]
    public string? Difficulty { get; set; }

    [StringLength(30)]
    public string? Sort { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
