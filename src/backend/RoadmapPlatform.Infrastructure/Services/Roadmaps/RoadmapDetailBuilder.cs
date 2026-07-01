using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapDetailBuilder(ApplicationDbContext dbContext)
{
    private const string PublishedStatus = "published";
    private const string ArchivedStatus = "archived";
    private const string MajorReleaseType = "major";
    private const string MinorReleaseType = "minor";

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
        var availableUpdate = enrollment == null
            ? await LoadAvailableUpdateAsync(version, userId, cancellationToken)
            : null;

        return new RoadmapDetailDto
        {
            RoadmapId = version.RoadmapId,
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
            EstimatedTotalHours = version.EstimatedTotalHours,
            EstimatedRequiredHours = estimatedTime.EstimatedRequiredHours,
            EstimatedOptionalHours = estimatedTime.EstimatedOptionalHours,
            LayoutDirection = version.LayoutDirection,
            LayoutAlgorithm = version.LayoutAlgorithm,
            CareerRole = MapCareerRole(roadmap.CareerRole),
            Enrollment = enrollment == null ? null : MapEnrollment(enrollment),
            AvailableUpdate = availableUpdate,
            TrackableNodeCount = progressSummary.TotalUnits,
            CompletedNodeCount = progressSummary.CompletedUnits,
            ProgressPercent = enrollment?.ProgressPercent ?? progressSummary.ProgressPercent,
            Nodes = nodes
                .OrderBy(n => n.OrderIndex)
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

    public async Task<RoadmapVersionUpdateDto?> LoadAvailableUpdateAsync(
        RoadmapVersion targetVersion,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue
            || !targetVersion.Status.Equals(PublishedStatus, StringComparison.OrdinalIgnoreCase)
            || (!targetVersion.ReleaseType.Equals(MajorReleaseType, StringComparison.OrdinalIgnoreCase)
                && !targetVersion.ReleaseType.Equals(MinorReleaseType, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var currentEnrollment = await dbContext.Set<RoadmapEnrollment>()
            .AsNoTracking()
            .Include(enrollment => enrollment.RoadmapVersion)
            .Where(enrollment =>
                enrollment.UserId == userId.Value
                && enrollment.RoadmapVersion.RoadmapId == targetVersion.RoadmapId
                && enrollment.RoadmapVersionId != targetVersion.RoadmapVersionId
                && (enrollment.RoadmapVersion.Status == PublishedStatus
                    || enrollment.RoadmapVersion.Status == ArchivedStatus))
            .OrderByDescending(enrollment => enrollment.RoadmapVersion.MajorVersion)
            .ThenByDescending(enrollment => enrollment.RoadmapVersion.MinorVersion)
            .ThenByDescending(enrollment => enrollment.RoadmapVersion.PatchVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentEnrollment == null || !IsNewerVersion(targetVersion, currentEnrollment.RoadmapVersion))
        {
            return null;
        }

        return new RoadmapVersionUpdateDto
        {
            RoadmapEnrollmentId = currentEnrollment.RoadmapEnrollmentId,
            CurrentRoadmapVersionId = currentEnrollment.RoadmapVersionId,
            CurrentVersionLabel = RoadmapVersionLabels.Format(currentEnrollment.RoadmapVersion),
            TargetRoadmapVersionId = targetVersion.RoadmapVersionId,
            TargetVersionLabel = RoadmapVersionLabels.Format(targetVersion),
            ReleaseType = targetVersion.ReleaseType,
            Title = targetVersion.Title,
            Description = targetVersion.Description,
            PublishedAt = targetVersion.PublishedAt,
            ProgressPercent = currentEnrollment.ProgressPercent
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
                    .GroupBy(nr => GetResourceDeduplicationKey(nr.LearningResource))
                    .Select(resourceGroup => resourceGroup
                        .OrderBy(nr => nr.LearningResource.ResourceType)
                        .ThenBy(nr => nr.LearningResource.Title)
                        .ThenBy(nr => nr.LearningResource.LearningResourceId)
                        .First())
                    .OrderBy(nr => nr.LearningResource.ResourceType)
                    .ThenBy(nr => nr.LearningResource.Title)
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

    private static string GetResourceDeduplicationKey(LearningResource resource)
    {
        return string.IsNullOrWhiteSpace(resource.Url)
            ? resource.LearningResourceId.ToString()
            : resource.Url.Trim().ToUpperInvariant();
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
            OrderIndex = node.OrderIndex,
            LayoutRole = node.LayoutRole,
            EstimatedHours = node.EstimatedHours,
            EstimatedRequiredHours = estimatedTime?.EstimatedRequiredHours ?? 0,
            EstimatedOptionalHours = estimatedTime?.EstimatedOptionalHours ?? 0,
            DifficultyLevel = node.DifficultyLevel,
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
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            SkippedAt = progress.SkippedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    private static bool IsNewerVersion(RoadmapVersion targetVersion, RoadmapVersion sourceVersion)
    {
        return targetVersion.MajorVersion > sourceVersion.MajorVersion
            || (targetVersion.MajorVersion == sourceVersion.MajorVersion
                && targetVersion.MinorVersion > sourceVersion.MinorVersion)
            || (targetVersion.MajorVersion == sourceVersion.MajorVersion
                && targetVersion.MinorVersion == sourceVersion.MinorVersion
                && targetVersion.PatchVersion > sourceVersion.PatchVersion);
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
