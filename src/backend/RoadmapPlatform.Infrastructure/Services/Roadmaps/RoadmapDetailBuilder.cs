using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapDetailBuilder(ApplicationDbContext dbContext)
{
    public async Task<RoadmapDetailDto> BuildAsync(
        Guid roadmapVersionId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Include(v => v.RoadmapNodes)
            .Include(v => v.RoadmapEdges)
            .Where(v => v.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        var roadmap = await dbContext.Set<Roadmap>()
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.RoadmapId == version.RoadmapId)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmap == null)
        {
            throw new KeyNotFoundException("Roadmap was not found.");
        }

        RoadmapEnrollment? enrollment = null;
        List<UserNodeProgress> progressRows = [];

        if (userId.HasValue)
        {
            enrollment = await dbContext.Set<RoadmapEnrollment>()
                .AsNoTracking()
                .Where(e =>
                    e.UserId == userId.Value &&
                    e.RoadmapVersionId == roadmapVersionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (enrollment != null)
            {
                progressRows = await dbContext.Set<UserNodeProgress>()
                    .AsNoTracking()
                    .Where(p => p.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
                    .ToListAsync(cancellationToken);
            }
        }

        var nodes = version.RoadmapNodes.ToList();
        var edges = version.RoadmapEdges.ToList();

        var nodeIds = nodes
            .Select(n => n.RoadmapNodeId)
            .ToList();

        var skillsByNodeId = await LoadSkillsByNodeIdAsync(nodeIds, cancellationToken);
        var resourcesByNodeId = await LoadResourcesByNodeIdAsync(nodeIds, cancellationToken);

        var progressByNodeId = progressRows.ToDictionary(p => p.RoadmapNodeId);
        var statusByNodeId = RoadmapProgressCalculator.CalculateStatuses(nodes, edges, progressByNodeId);
        var progressSummary = RoadmapProgressCalculator.CalculateRoadmapProgress(nodes, edges, statusByNodeId);
        var estimatedTime = RoadmapEstimatedTimeCalculator.Calculate(nodes, edges);

        return new RoadmapDetailDto
        {
            RoadmapId = version.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.CareerRole.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            RoadmapType = roadmap.RoadmapType,
            SourceType = roadmap.SourceType,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            EstimatedTotalHours = version.EstimatedTotalHours,
            EstimatedRequiredHours = estimatedTime.EstimatedRequiredHours,
            EstimatedOptionalHours = estimatedTime.EstimatedOptionalHours,
            GenerationStatus = version.GenerationStatus,
            GenerationModel = version.GenerationModel,
            GenerationError = version.GenerationError,
            LayoutDirection = version.LayoutDirection,
            LayoutAlgorithm = version.LayoutAlgorithm,
            CareerRole = MapCareerRole(roadmap.CareerRole),
            Enrollment = enrollment == null ? null : MapEnrollment(enrollment),
            TrackableNodeCount = progressSummary.TotalUnits,
            CompletedNodeCount = progressSummary.CompletedUnits,
            ProgressPercent = enrollment?.ProgressPercent ?? progressSummary.ProgressPercent,
            Nodes = nodes
                .OrderBy(n => n.LayoutRank ?? int.MaxValue)
                .ThenBy(n => n.LayoutOrder)
                .ThenBy(n => n.PositionY ?? 0)
                .ThenBy(n => n.PositionX ?? 0)
                .ThenBy(n => n.OrderIndex)
                .ThenBy(n => n.Title)
                .Select(n => MapNode(
                    n,
                    progressByNodeId,
                    statusByNodeId.GetValueOrDefault(n.RoadmapNodeId, "pending"),
                    estimatedTime.NodeEstimates.GetValueOrDefault(n.RoadmapNodeId),
                    skillsByNodeId,
                    resourcesByNodeId))
                .ToList(),
            Edges = edges
                .OrderBy(e => e.EdgeType)
                .ThenBy(e => e.DependencyType)
                .Select(MapEdge)
                .ToList()
        };
    }

    public static CareerRoleDto MapCareerRole(CareerRole role)
    {
        return new CareerRoleDto
        {
            CareerRoleId = role.CareerRoleId,
            Name = role.Name,
            Slug = role.Slug,
            Description = role.Description,
            Category = role.Category
        };
    }

    public static RoadmapEnrollmentDto MapEnrollment(RoadmapEnrollment enrollment)
    {
        return new RoadmapEnrollmentDto
        {
            RoadmapEnrollmentId = enrollment.RoadmapEnrollmentId,
            RoadmapVersionId = enrollment.RoadmapVersionId,
            Status = enrollment.Status,
            ProgressPercent = enrollment.ProgressPercent,
            StartedAt = enrollment.StartedAt,
            CompletedAt = enrollment.CompletedAt
        };
    }

    private async Task<Dictionary<Guid, List<SkillDto>>> LoadSkillsByNodeIdAsync(
        IReadOnlyCollection<Guid> nodeIds,
        CancellationToken cancellationToken)
    {
        if (nodeIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Include(ns => ns.Skill)
            .Where(ns => nodeIds.Contains(ns.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(ns => ns.RoadmapNodeId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(ns => ns.Skill.Name)
                    .Select(ns => new SkillDto
                    {
                        SkillId = ns.Skill.SkillId,
                        Name = ns.Skill.Name,
                        Slug = ns.Skill.Slug,
                        Category = ns.Skill.Category
                    })
                    .ToList());
    }

    private async Task<Dictionary<Guid, List<LearningResourceDto>>> LoadResourcesByNodeIdAsync(
        IReadOnlyCollection<Guid> nodeIds,
        CancellationToken cancellationToken)
    {
        if (nodeIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Include(nr => nr.LearningResource)
            .Where(nr => nodeIds.Contains(nr.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(nr => nr.RoadmapNodeId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(nr => nr.OrderIndex)
                    .Select(nr => new LearningResourceDto
                    {
                        ResourceId = nr.LearningResource.LearningResourceId,
                        Title = nr.LearningResource.Title,
                        Url = nr.LearningResource.Url,
                        ResourceType = nr.LearningResource.ResourceType,
                        Description = nr.LearningResource.Description,
                        Provider = nr.LearningResource.Provider,
                        DifficultyLevel = nr.LearningResource.DifficultyLevel,
                        LanguageCode = nr.LearningResource.LanguageCode
                    })
                    .ToList());
    }

    private static RoadmapNodeDto MapNode(
        RoadmapNode node,
        IReadOnlyDictionary<Guid, UserNodeProgress> progressByNodeId,
        string effectiveStatus,
        RoadmapNodeEstimatedTime? estimatedTime,
        IReadOnlyDictionary<Guid, List<SkillDto>> skillsByNodeId,
        IReadOnlyDictionary<Guid, List<LearningResourceDto>> resourcesByNodeId)
    {
        progressByNodeId.TryGetValue(node.RoadmapNodeId, out var progress);

        return new RoadmapNodeDto
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
            Reason = node.Reason,
            OrderIndex = node.OrderIndex,
            LayoutRole = node.LayoutRole,
            LayoutGroup = node.LayoutGroup,
            LayoutRank = node.LayoutRank,
            LayoutOrder = node.LayoutOrder,
            EstimatedHours = node.EstimatedHours,
            EstimatedRequiredHours = estimatedTime?.EstimatedRequiredHours ?? 0,
            EstimatedOptionalHours = estimatedTime?.EstimatedOptionalHours ?? 0,
            DifficultyLevel = node.DifficultyLevel,
            Priority = node.Priority,
            PositionX = node.PositionX,
            PositionY = node.PositionY,
            Metadata = ToJsonElement(node.Metadata),
            IsRequired = node.IsRequired,
            IsTrackable = node.IsTrackable,
            LearningOutcomes = DeserializeStringArray(node.LearningOutcomes),
            CompletionCriteria = DeserializeStringArray(node.CompletionCriteria),
            Skills = skillsByNodeId.GetValueOrDefault(node.RoadmapNodeId) ?? [],
            Resources = resourcesByNodeId.GetValueOrDefault(node.RoadmapNodeId) ?? [],
            Progress = MapNodeProgress(node.RoadmapNodeId, progress, effectiveStatus)
        };
    }

    private static RoadmapEdgeDto MapEdge(RoadmapEdge edge)
    {
        return new RoadmapEdgeDto
        {
            RoadmapEdgeId = edge.RoadmapEdgeId,
            FromNodeId = edge.FromNodeId,
            ToNodeId = edge.ToNodeId,
            EdgeType = edge.EdgeType,
            DependencyType = edge.DependencyType,
            Condition = ToJsonElement(edge.Condition)
        };
    }

    private static UserNodeProgressDto MapNodeProgress(
        Guid roadmapNodeId,
        UserNodeProgress? progress,
        string effectiveStatus)
    {
        if (progress == null)
        {
            return new UserNodeProgressDto
            {
                RoadmapNodeId = roadmapNodeId,
                Status = effectiveStatus,
                IsComputed = true
            };
        }

        return new UserNodeProgressDto
        {
            UserNodeProgressId = progress.UserNodeProgressId,
            RoadmapEnrollmentId = progress.RoadmapEnrollmentId,
            RoadmapNodeId = progress.RoadmapNodeId,
            Status = effectiveStatus,
            IsComputed = progress.Status != effectiveStatus,
            EvidenceUrl = progress.EvidenceUrl,
            LearnerNote = progress.LearnerNote,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            SkippedAt = progress.SkippedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    private static List<string> DeserializeStringArray(object? value)
    {
        if (value == null)
        {
            return [];
        }

        if (value is JsonDocument jsonDocument)
        {
            return jsonDocument.RootElement.ValueKind == JsonValueKind.Array
                ? jsonDocument.RootElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                : [];
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Array
                ? element.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                : [];
        }

        var raw = value.ToString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static JsonElement? ToJsonElement(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is JsonDocument jsonDocument)
        {
            return jsonDocument.RootElement.Clone();
        }

        if (value is JsonElement element)
        {
            return element.Clone();
        }

        var raw = value.ToString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var parsedDocument = JsonDocument.Parse(raw);
            return parsedDocument.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}