using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLearningModuleLessonRequestDto
{
    private string? _summary;
    private decimal? _estimatedHours;

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }

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
