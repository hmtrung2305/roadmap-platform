using RoadmapPlatform.Application.DTOs.ContentRoadmaps;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

internal static class ContentRoadmapMapper
{
    public static ContentRoadmapSummaryDto MapSummary(
        Roadmap roadmap,
        RoadmapVersion version,
        RoadmapVersionAggregate? aggregate)
    {
        return new ContentRoadmapSummaryDto
        {
            RoadmapId = roadmap.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.CareerRole.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            RoadmapType = roadmap.RoadmapType,
            SourceType = roadmap.SourceType,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            Status = version.Status,
            EstimatedTotalHours = version.EstimatedTotalHours,
            NodeCount = aggregate?.NodeCount ?? 0,
            TrackableNodeCount = aggregate?.TrackableNodeCount ?? 0,
            ResourceMappingCount = aggregate?.ResourceMappingCount ?? 0,
            SkillMappingCount = aggregate?.SkillMappingCount ?? 0,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(roadmap.CareerRole)
        };
    }

    public static ContentRoadmapDetailDto MapDetail(
        Roadmap roadmap,
        RoadmapVersion version,
        List<ContentRoadmapNodeDto> nodes,
        List<ContentRoadmapEdgeDto> edges,
        RoadmapVersionAggregate aggregate)
    {
        return new ContentRoadmapDetailDto
        {
            RoadmapId = roadmap.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.CareerRole.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            RoadmapType = roadmap.RoadmapType,
            SourceType = roadmap.SourceType,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            Status = version.Status,
            EstimatedTotalHours = version.EstimatedTotalHours,
            LayoutDirection = version.LayoutDirection,
            LayoutAlgorithm = version.LayoutAlgorithm,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(roadmap.CareerRole),
            Versions = roadmap.RoadmapVersions
                .OrderByDescending(item => item.VersionNumber)
                .Select(item => new ContentRoadmapVersionSummaryDto
                {
                    RoadmapVersionId = item.RoadmapVersionId,
                    VersionNumber = item.VersionNumber,
                    Status = item.Status,
                    Title = item.Title,
                    CreatedAt = item.CreatedAt,
                    PublishedAt = item.PublishedAt
                })
                .ToList(),
            Nodes = nodes,
            Edges = edges,
            NodeCount = aggregate.NodeCount,
            TrackableNodeCount = aggregate.TrackableNodeCount,
            ResourceMappingCount = aggregate.ResourceMappingCount,
            SkillMappingCount = aggregate.SkillMappingCount
        };
    }

    public static ContentRoadmapNodeDto MapNode(
        RoadmapNode node,
        List<SkillDto> skills,
        List<LearningResourceDto> resources)
    {
        return new ContentRoadmapNodeDto
        {
            RoadmapNodeId = node.RoadmapNodeId,
            ParentNodeId = node.ParentNodeId,
            Slug = node.Slug,
            NodeType = node.NodeType,
            CheckpointType = node.CheckpointType,
            SelectionType = node.SelectionType,
            RequiredCount = node.RequiredCount,
            Title = node.Title,
            Description = node.Description,
            OrderIndex = node.OrderIndex,
            LayoutRole = node.LayoutRole,
            LayoutGroup = node.LayoutGroup,
            LayoutRank = node.LayoutRank,
            LayoutOrder = node.LayoutOrder,
            EstimatedHours = node.EstimatedHours,
            DifficultyLevel = node.DifficultyLevel,
            IsRequired = node.IsRequired,
            IsTrackable = node.IsTrackable,
            Skills = skills,
            Resources = resources
        };
    }

    public static ContentRoadmapEdgeDto MapEdge(RoadmapEdge edge)
    {
        return new ContentRoadmapEdgeDto
        {
            RoadmapEdgeId = edge.RoadmapEdgeId,
            RoadmapVersionId = edge.RoadmapVersionId,
            FromNodeId = edge.FromNodeId,
            ToNodeId = edge.ToNodeId,
            EdgeType = edge.EdgeType,
            DependencyType = edge.DependencyType,
            Condition = edge.Condition
        };
    }
}

internal sealed record RoadmapVersionAggregate(
    int NodeCount,
    int TrackableNodeCount,
    int ResourceMappingCount,
    int SkillMappingCount);
