using System.Text.Json;

namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapNodeDetailDto
{
    public Guid RoadmapNodeId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? CheckpointType { get; set; }
    public string? SelectionType { get; set; }
    public int? RequiredCount { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Reason { get; set; }
    public int? EstimatedHours { get; set; }
    public decimal EstimatedRequiredHours { get; set; }
    public decimal EstimatedOptionalHours { get; set; }
    public string? DifficultyLevel { get; set; }
    public int Priority { get; set; }
    public JsonElement? Metadata { get; set; }
    public bool IsRequired { get; set; }
    public bool IsTrackable { get; set; }
    public List<string> LearningOutcomes { get; set; } = [];
    public List<string> CompletionCriteria { get; set; } = [];
    public List<SkillDto> Skills { get; set; } = [];
    public List<LearningResourceDto> Resources { get; set; } = [];
    public List<RoadmapLearningModuleDto> LearningModules { get; set; } = [];
    public List<RoadmapChildSummaryDto> Children { get; set; } = [];
    public UserNodeProgressDto Progress { get; set; } = new();
}
