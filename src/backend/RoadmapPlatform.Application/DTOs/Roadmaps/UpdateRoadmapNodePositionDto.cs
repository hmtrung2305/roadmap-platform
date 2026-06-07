namespace RoadmapPlatform.Application.DTOs.Roadmaps;

public sealed class UpdateRoadmapNodePositionDto
{
    public Guid RoadmapNodeId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
}
