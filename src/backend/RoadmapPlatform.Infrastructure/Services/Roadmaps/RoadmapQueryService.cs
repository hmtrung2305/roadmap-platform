using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapQueryService(
    ApplicationDbContext dbContext,
    RoadmapDetailBuilder detailBuilder) : IRoadmapQueryService
{
    public async Task<IReadOnlyList<RoadmapSummaryDto>> GetPublishedRoadmapsAsync(CancellationToken cancellationToken)
    {
        var allRows = await (
            from version in dbContext.Set<RoadmapVersion>().AsNoTracking()
            join roadmap in dbContext.Set<Roadmap>().AsNoTracking()
                on version.RoadmapId equals roadmap.RoadmapId
            join careerRole in dbContext.Set<CareerRole>().AsNoTracking()
                on roadmap.CareerRoleId equals careerRole.CareerRoleId
            where version.Status == "published" &&
                  roadmap.Visibility == "public"
            select new
            {
                Roadmap = roadmap,
                CareerRole = careerRole,
                PublishedVersion = version
            })
            .ToListAsync(cancellationToken);

        var rows = allRows
            .GroupBy(x => x.Roadmap.RoadmapId)
            .Select(g => g
                .OrderByDescending(x => x.PublishedVersion.VersionNumber)
                .First())
            .OrderBy(x => x.CareerRole.Name)
            .ThenBy(x => x.Roadmap.Title)
            .ToList();

        var versionIds = rows
            .Select(x => x.PublishedVersion.RoadmapVersionId)
            .ToList();

        var nodesByVersionId = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(n => versionIds.Contains(n.RoadmapVersionId))
            .ToListAsync(cancellationToken);

        var edgesByVersionId = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(e => versionIds.Contains(e.RoadmapVersionId))
            .ToListAsync(cancellationToken);

        var nodesLookup = nodesByVersionId
            .GroupBy(n => n.RoadmapVersionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RoadmapNode>)g.ToList());

        var edgesLookup = edgesByVersionId
            .GroupBy(e => e.RoadmapVersionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RoadmapEdge>)g.ToList());

        var estimatedTimeByVersionId = versionIds.ToDictionary(
            id => id,
            id => RoadmapEstimatedTimeCalculator.Calculate(
                nodesLookup.GetValueOrDefault(id) ?? [],
                edgesLookup.GetValueOrDefault(id) ?? []));

        return rows.Select(x => new RoadmapSummaryDto
        {
            RoadmapId = x.Roadmap.RoadmapId,
            RoadmapVersionId = x.PublishedVersion.RoadmapVersionId,
            Slug = x.CareerRole.Slug,
            Title = x.PublishedVersion.Title,
            Description = x.PublishedVersion.Description ?? x.Roadmap.Description,
            Visibility = x.Roadmap.Visibility,
            EstimatedTotalHours = x.PublishedVersion.EstimatedTotalHours,
            EstimatedRequiredHours = estimatedTimeByVersionId.GetValueOrDefault(x.PublishedVersion.RoadmapVersionId)?.EstimatedRequiredHours ?? 0,
            EstimatedOptionalHours = estimatedTimeByVersionId.GetValueOrDefault(x.PublishedVersion.RoadmapVersionId)?.EstimatedOptionalHours ?? 0,
            LayoutDirection = x.PublishedVersion.LayoutDirection,
            LayoutAlgorithm = x.PublishedVersion.LayoutAlgorithm,
            NodeCount = nodesLookup.GetValueOrDefault(x.PublishedVersion.RoadmapVersionId)?.Count ?? 0,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(x.CareerRole)
        }).ToList();
    }

    public async Task<RoadmapDetailDto> GetPublishedRoadmapBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var roadmapVersionId = await GetPublishedVersionIdBySlugAsync(slug, cancellationToken);
        return await detailBuilder.BuildAsync(roadmapVersionId, userId, cancellationToken);
    }

    public async Task<RoadmapDetailDto> GetRoadmapDetailByVersionIdAsync(
        Guid roadmapVersionId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap version was not provided.");
        }

        return await detailBuilder.BuildAsync(roadmapVersionId, userId, cancellationToken);
    }

    public async Task<RoadmapGraphDto> GetPublishedRoadmapGraphBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var roadmapVersionId = await GetPublishedVersionIdBySlugAsync(slug, cancellationToken);
        return await BuildGraphAsync(roadmapVersionId, userId, cancellationToken);
    }

    public async Task<RoadmapNodeDetailDto> GetRoadmapNodeDetailAsync(
        Guid roadmapVersionId,
        Guid roadmapNodeId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap version was not provided.");
        }

        if (roadmapNodeId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap node was not provided.");
        }

        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(n => n.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        var node = nodes.FirstOrDefault(n => n.RoadmapNodeId == roadmapNodeId);

        if (node == null)
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }

        var edges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(e => e.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        var enrollment = userId.HasValue
            ? await dbContext.Set<RoadmapEnrollment>()
                .AsNoTracking()
                .Where(e =>
                    e.UserId == userId.Value &&
                    e.RoadmapVersionId == roadmapVersionId)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var progressRows = enrollment == null
            ? new List<UserNodeProgress>()
            : await dbContext.Set<UserNodeProgress>()
                .AsNoTracking()
                .Where(p => p.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
                .ToListAsync(cancellationToken);

        var skillsByNodeId = await LoadSkillsByNodeIdAsync([roadmapNodeId], cancellationToken);
        var resourcesByNodeId = await LoadResourcesByNodeIdAsync([roadmapNodeId], cancellationToken);
        var skillIds = skillsByNodeId
            .GetValueOrDefault(roadmapNodeId)?
            .Select(skill => skill.SkillId)
            .ToList() ?? [];

        var learningModules = await LoadPublishedLearningModulesBySkillIdsAsync(skillIds, cancellationToken);

        var progressByNodeId = progressRows.ToDictionary(p => p.RoadmapNodeId);
        var statusByNodeId = RoadmapProgressCalculator.CalculateStatuses(nodes, edges, progressByNodeId);
        var status = statusByNodeId.GetValueOrDefault(node.RoadmapNodeId, "pending");
        var estimatedTime = RoadmapEstimatedTimeCalculator.Calculate(nodes, edges);

        return MapNodeDetail(
            node,
            nodes,
            edges,
            progressByNodeId,
            statusByNodeId,
            status,
            estimatedTime.NodeEstimates.GetValueOrDefault(node.RoadmapNodeId),
            skillsByNodeId,
            resourcesByNodeId,
            learningModules);
    }

    private async Task<Guid> GetPublishedVersionIdBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new InvalidOperationException("Roadmap slug was not provided.");
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var roadmapVersionId = await (
            from version in dbContext.Set<RoadmapVersion>().AsNoTracking()
            join roadmap in dbContext.Set<Roadmap>().AsNoTracking()
                on version.RoadmapId equals roadmap.RoadmapId
            join careerRole in dbContext.Set<CareerRole>().AsNoTracking()
                on roadmap.CareerRoleId equals careerRole.CareerRoleId
            where version.Status == "published" &&
                  roadmap.Visibility == "public" &&
                  careerRole.Slug == normalizedSlug
            orderby version.VersionNumber descending
            select version.RoadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmapVersionId == Guid.Empty)
        {
            throw new KeyNotFoundException("Roadmap was not found.");
        }

        return roadmapVersionId;
    }

    private async Task<RoadmapGraphDto> BuildGraphAsync(
        Guid roadmapVersionId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(v => v.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(n => n.RoadmapVersionId == roadmapVersionId)
            .Select(n => new RoadmapNode
            {
                RoadmapNodeId = n.RoadmapNodeId,
                RoadmapVersionId = n.RoadmapVersionId,
                ParentNodeId = n.ParentNodeId,
                Slug = n.Slug,
                NodeType = n.NodeType,
                CheckpointType = n.CheckpointType,
                SelectionType = n.SelectionType,
                RequiredCount = n.RequiredCount,
                Title = n.Title,
                OrderIndex = n.OrderIndex,
                LayoutRole = n.LayoutRole,
                EstimatedHours = n.EstimatedHours,
                IsRequired = n.IsRequired,
                IsTrackable = n.IsTrackable
            })
            .ToListAsync(cancellationToken);

        var edges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(e => e.RoadmapVersionId == roadmapVersionId)
            .Select(e => new RoadmapEdge
            {
                RoadmapEdgeId = e.RoadmapEdgeId,
                RoadmapVersionId = e.RoadmapVersionId,
                FromNodeId = e.FromNodeId,
                ToNodeId = e.ToNodeId,
                EdgeType = e.EdgeType,
                DependencyType = e.DependencyType
            })
            .ToListAsync(cancellationToken);

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

        var progressByNodeId = progressRows.ToDictionary(p => p.RoadmapNodeId);
        var statusByNodeId = RoadmapProgressCalculator.CalculateStatuses(nodes, edges, progressByNodeId);
        var progressSummary = RoadmapProgressCalculator.CalculateRoadmapProgress(nodes, edges, statusByNodeId);
        var estimatedTime = RoadmapEstimatedTimeCalculator.Calculate(nodes, edges);

        return new RoadmapGraphDto
        {
            RoadmapId = version.RoadmapId,
            RoadmapVersionId = version.RoadmapVersionId,
            Slug = roadmap.CareerRole.Slug,
            Title = version.Title,
            Description = version.Description ?? roadmap.Description,
            Visibility = roadmap.Visibility,
            VersionNumber = version.VersionNumber,
            EstimatedTotalHours = version.EstimatedTotalHours,
            EstimatedRequiredHours = estimatedTime.EstimatedRequiredHours,
            EstimatedOptionalHours = estimatedTime.EstimatedOptionalHours,
            LayoutDirection = version.LayoutDirection,
            LayoutAlgorithm = version.LayoutAlgorithm,
            CareerRole = RoadmapDetailBuilder.MapCareerRole(roadmap.CareerRole),
            Enrollment = enrollment == null ? null : RoadmapDetailBuilder.MapEnrollment(enrollment),
            TrackableNodeCount = progressSummary.TotalUnits,
            CompletedNodeCount = progressSummary.CompletedUnits,
            ProgressPercent = enrollment?.ProgressPercent ?? progressSummary.ProgressPercent,
            Nodes = nodes
                .OrderBy(n => n.OrderIndex)
                .ThenBy(n => n.Title)
                .Select(n => MapGraphNode(
                    n,
                    progressByNodeId,
                    statusByNodeId.GetValueOrDefault(n.RoadmapNodeId, "pending"),
                    estimatedTime.NodeEstimates.GetValueOrDefault(n.RoadmapNodeId)))
                .ToList(),
            Edges = edges
                .OrderBy(e => e.EdgeType)
                .ThenBy(e => e.DependencyType)
                .Select(e => new RoadmapGraphEdgeDto
                {
                    RoadmapEdgeId = e.RoadmapEdgeId,
                    FromNodeId = e.FromNodeId,
                    ToNodeId = e.ToNodeId,
                    EdgeType = e.EdgeType,
                    DependencyType = e.DependencyType
                })
                .ToList()
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


    private async Task<List<RoadmapLearningModuleDto>> LoadPublishedLearningModulesBySkillIdsAsync(
        IReadOnlyCollection<Guid> skillIds,
        CancellationToken cancellationToken)
    {
        if (skillIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Set<SkillModule>()
            .AsNoTracking()
            .Where(module =>
                skillIds.Contains(module.SkillId) &&
                module.Status == "published")
            .OrderBy(module => module.Title)
            .Select(module => new RoadmapLearningModuleDto
            {
                SkillModuleId = module.SkillModuleId,
                SkillId = module.SkillId,
                Title = module.Title,
                Slug = module.Slug,
                DifficultyLevel = module.DifficultyLevel,
                EstimatedHours = module.EstimatedHours,
                LessonCount = module.SkillModuleLessons.Count,
                QuestionCount = module.SkillModuleQuiz == null
                    ? 0
                    : module.SkillModuleQuiz.SkillModuleQuizQuestions.Count,
                Provider = "Roadmap Platform"
            })
            .ToListAsync(cancellationToken);
    }

    private static RoadmapGraphNodeDto MapGraphNode(
        RoadmapNode node,
        IReadOnlyDictionary<Guid, UserNodeProgress> progressByNodeId,
        string effectiveStatus,
        RoadmapNodeEstimatedTime? estimatedTime)
    {
        progressByNodeId.TryGetValue(node.RoadmapNodeId, out var progress);

        return new RoadmapGraphNodeDto
        {
            RoadmapNodeId = node.RoadmapNodeId,
            ParentNodeId = node.ParentNodeId,
            Slug = node.Slug,
            NodeType = node.NodeType,
            CheckpointType = node.CheckpointType,
            SelectionType = node.SelectionType,
            RequiredCount = node.RequiredCount,
            Title = node.Title,
            OrderIndex = node.OrderIndex,
            LayoutRole = node.LayoutRole,
            EstimatedRequiredHours = estimatedTime?.EstimatedRequiredHours ?? 0,
            EstimatedOptionalHours = estimatedTime?.EstimatedOptionalHours ?? 0,
            IsRequired = node.IsRequired,
            IsTrackable = node.IsTrackable,
            Progress = MapNodeProgress(node.RoadmapNodeId, progress, effectiveStatus)
        };
    }

    private static RoadmapNodeDetailDto MapNodeDetail(
        RoadmapNode node,
        IReadOnlyList<RoadmapNode> allNodes,
        IReadOnlyList<RoadmapEdge> edges,
        IReadOnlyDictionary<Guid, UserNodeProgress> progressByNodeId,
        IReadOnlyDictionary<Guid, string> statusByNodeId,
        string effectiveStatus,
        RoadmapNodeEstimatedTime? estimatedTime,
        IReadOnlyDictionary<Guid, List<SkillDto>> skillsByNodeId,
        IReadOnlyDictionary<Guid, List<LearningResourceDto>> resourcesByNodeId,
        IReadOnlyList<RoadmapLearningModuleDto> learningModules)
    {
        progressByNodeId.TryGetValue(node.RoadmapNodeId, out var progress);

        var childIds = edges
            .Where(e =>
                e.FromNodeId == node.RoadmapNodeId &&
                e.DependencyType == "required" &&
                e.EdgeType is "contains" or "choice")
            .Select(e => e.ToNodeId)
            .ToHashSet();

        var children = childIds.Count > 0
            ? allNodes.Where(n => childIds.Contains(n.RoadmapNodeId))
            : allNodes.Where(n => n.ParentNodeId == node.RoadmapNodeId);

        return new RoadmapNodeDetailDto
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
            LearningModules = learningModules.ToList(),
            Children = children
                .Where(n => n.IsRequired)
                .OrderBy(n => n.OrderIndex)
                .Select(n => new RoadmapChildSummaryDto
                {
                    RoadmapNodeId = n.RoadmapNodeId,
                    Slug = n.Slug,
                    Title = n.Title,
                    NodeType = n.NodeType,
                    Status = statusByNodeId.GetValueOrDefault(n.RoadmapNodeId, "pending"),
                    IsRequired = n.IsRequired
                })
                .ToList(),
            Progress = MapNodeProgress(node.RoadmapNodeId, progress, effectiveStatus)
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