using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class BulkUploadLessonsRequestDto
{
    [MinLength(1)]
    public List<BulkUploadLessonItemDto> Lessons { get; set; } = [];
}

public sealed class BulkUploadLessonItemDto
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Slug { get; set; }

    public string? Summary { get; set; }

    public int OrderIndex { get; set; }

    public decimal? EstimatedHours { get; set; }
}
