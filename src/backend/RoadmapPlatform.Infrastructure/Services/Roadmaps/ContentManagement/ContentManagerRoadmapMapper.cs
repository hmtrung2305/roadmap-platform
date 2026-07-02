using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapMapper
{
    public static ContentRoadmapSummaryDto MapSummary(
        Roadmap roadmap,
        RoadmapVersion version,
        RoadmapVersionAggregate? aggregate,
        IReadOnlyList<RoadmapVersionReviewEvent>? reviewEvents = null)
    {
        var mappedReviewEvents = MapReviewEvents(reviewEvents);

        return new ContentRoadmapSummaryDto
        {
            RoadmapId = roadmap.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            MajorVersion = version.MajorVersion,
            MinorVersion = version.MinorVersion,
            PatchVersion = version.PatchVersion,
            VersionLabel = RoadmapVersionLabels.Format(version),
            ReleaseType = version.ReleaseType,
            CreatedFromVersionId = version.CreatedFromVersionId,
            Status = version.Status,
            EstimatedTotalHours = version.EstimatedTotalHours,
            NodeCount = aggregate?.NodeCount ?? 0,
            TrackableNodeCount = aggregate?.TrackableNodeCount ?? 0,
            ResourceMappingCount = aggregate?.ResourceMappingCount ?? 0,
            SkillMappingCount = aggregate?.SkillMappingCount ?? 0,
            CreatedAt = version.CreatedAt,
            UpdatedAt = version.UpdatedAt,
            PublishedAt = version.PublishedAt,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(roadmap.CareerRole),
            LatestReviewEvent = mappedReviewEvents.FirstOrDefault(),
            ReviewEvents = mappedReviewEvents
        };
    }

    public static ContentRoadmapDetailDto MapDetail(
        Roadmap roadmap,
        RoadmapVersion version,
        List<ContentRoadmapNodeDto> nodes,
        List<ContentRoadmapEdgeDto> edges,
        RoadmapVersionAggregate aggregate,
        IReadOnlyList<RoadmapVersionReviewEvent>? reviewEvents = null)
    {
        var mappedReviewEvents = MapReviewEvents(reviewEvents);

        return new ContentRoadmapDetailDto
        {
            RoadmapId = roadmap.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            MajorVersion = version.MajorVersion,
            MinorVersion = version.MinorVersion,
            PatchVersion = version.PatchVersion,
            VersionLabel = RoadmapVersionLabels.Format(version),
            ReleaseType = version.ReleaseType,
            CreatedFromVersionId = version.CreatedFromVersionId,
            Status = version.Status,
            EstimatedTotalHours = version.EstimatedTotalHours,
            LayoutDirection = version.LayoutDirection,
            LayoutAlgorithm = version.LayoutAlgorithm,
            CreatedAt = version.CreatedAt,
            UpdatedAt = version.UpdatedAt,
            PublishedAt = version.PublishedAt,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(roadmap.CareerRole),
            Versions = RoadmapVersionLabels.OrderNewestFirst(roadmap.RoadmapVersions)
                .Select(item => new ContentRoadmapVersionSummaryDto
                {
                    RoadmapVersionId = item.RoadmapVersionId,
                    VersionNumber = item.VersionNumber,
                    MajorVersion = item.MajorVersion,
                    MinorVersion = item.MinorVersion,
                    PatchVersion = item.PatchVersion,
                    VersionLabel = RoadmapVersionLabels.Format(item),
                    ReleaseType = item.ReleaseType,
                    CreatedFromVersionId = item.CreatedFromVersionId,
                    Status = item.Status,
                    Title = item.Title,
                    UpdatedAt = item.UpdatedAt,
                    CreatedAt = item.CreatedAt,
                    PublishedAt = item.PublishedAt
                })
                .ToList(),
            Nodes = nodes,
            Edges = edges,
            NodeCount = aggregate.NodeCount,
            TrackableNodeCount = aggregate.TrackableNodeCount,
            ResourceMappingCount = aggregate.ResourceMappingCount,
            SkillMappingCount = aggregate.SkillMappingCount,
            LatestReviewEvent = mappedReviewEvents.FirstOrDefault(),
            ReviewEvents = mappedReviewEvents
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
            EstimatedHours = node.EstimatedHours,
            DifficultyLevel = node.DifficultyLevel,
            IsRequired = node.IsRequired,
            IsTrackable = node.IsTrackable,
            Metadata = ContentManagerRoadmapNodeContent.ToJsonElement(node.Metadata),
            LearningOutcomes = ContentManagerRoadmapNodeContent.DeserializeStringArray(node.LearningOutcomes),
            CompletionCriteria = ContentManagerRoadmapNodeContent.DeserializeStringArray(node.CompletionCriteria),
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

    private static List<ContentRoadmapReviewEventDto> MapReviewEvents(
        IReadOnlyList<RoadmapVersionReviewEvent>? reviewEvents)
    {
        return (reviewEvents ?? [])
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new ContentRoadmapReviewEventDto
            {
                RoadmapVersionReviewEventId = item.RoadmapVersionReviewEventId,
                RoadmapVersionId = item.RoadmapVersionId,
                ActorUserId = item.ActorUserId,
                ActorUsername = item.ActorUser?.Username,
                ActorDisplayName = item.ActorUser?.UserProfile?.DisplayName,
                EventType = item.EventType,
                Message = item.Message,
                CreatedAt = item.CreatedAt
            })
            .ToList();
    }
}

internal sealed record RoadmapVersionAggregate(
    int NodeCount,
    int TrackableNodeCount,
    int ResourceMappingCount,
    int SkillMappingCount);
