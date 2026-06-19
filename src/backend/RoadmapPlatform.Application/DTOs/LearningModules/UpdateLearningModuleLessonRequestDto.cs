using RoadmapPlatform.Application.Constants;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLearningModuleLessonRequestDto
{
    private string? _summary;
    private decimal? _estimatedHours;

    [MaxLength(LearningModuleAuthoringLimits.LessonTitleMaxLength)]
    public string? Title { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.LessonSlugMaxLength)]
    public string? Slug { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.LessonSummaryMaxLength)]
    public string? Summary
    {
        get => _summary;
        set
        {
            _summary = value;
            SummaryIsSpecified = true;
        }
    }

    [JsonIgnore]
    public bool SummaryIsSpecified { get; private set; }

    public decimal? EstimatedHours
    {
        get => _estimatedHours;
        set
        {
            _estimatedHours = value;
            EstimatedHoursIsSpecified = true;
        }
    }

    [JsonIgnore]
    public bool EstimatedHoursIsSpecified { get; private set; }
}
