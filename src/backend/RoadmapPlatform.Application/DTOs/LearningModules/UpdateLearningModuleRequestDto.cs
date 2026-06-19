using RoadmapPlatform.Application.Constants;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLearningModuleRequestDto
{
    private string? _description;
    private string? _difficultyLevel;
    private decimal? _estimatedHours;
    private Dictionary<string, object?>? _metadata;

    public Guid? SkillId { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.ModuleTitleMaxLength)]
    public string? Title { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.ModuleSlugMaxLength)]
    public string? Slug { get; set; }

    [MaxLength(LearningModuleAuthoringLimits.ModuleDescriptionMaxLength)]
    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            DescriptionIsSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DescriptionIsSpecified { get; private set; }

    [MaxLength(LearningModuleAuthoringLimits.DifficultyLevelMaxLength)]
    public string? DifficultyLevel
    {
        get => _difficultyLevel;
        set
        {
            _difficultyLevel = value;
            DifficultyLevelIsSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DifficultyLevelIsSpecified { get; private set; }

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

    public Dictionary<string, object?>? Metadata
    {
        get => _metadata;
        set
        {
            _metadata = value;
            MetadataIsSpecified = true;
        }
    }

    [JsonIgnore]
    public bool MetadataIsSpecified { get; private set; }
}
