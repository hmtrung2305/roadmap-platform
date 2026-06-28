namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class RoadmapSummaryDto
{
    public Guid RoadmapId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public int? EstimatedTotalHours { get; set; }
    public decimal EstimatedRequiredHours { get; set; }
    public decimal EstimatedOptionalHours { get; set; }
    public string LayoutDirection { get; set; } = string.Empty;
    public string? LayoutAlgorithm { get; set; }
    public int NodeCount { get; set; }
    public CareerRoleDto CareerRole { get; set; } = new();
}
