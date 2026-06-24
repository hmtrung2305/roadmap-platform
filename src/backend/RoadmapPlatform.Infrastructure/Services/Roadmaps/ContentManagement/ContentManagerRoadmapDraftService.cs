using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapDraftService(
    ApplicationDbContext dbContext,
    ContentManagerRoadmapQueryService queryService,
    ContentManagerRoadmapValidationService validationService)
{
    private const string DraftStatus = "draft";
    private const string PublishedStatus = "published";
    private const string ArchivedStatus = "archived";

    public async Task<ContentRoadmapDetailDto> CloneRoadmapVersionToDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var sourceVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        if (!sourceVersion.Status.Equals(PublishedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only published roadmap versions can be copied into a draft.");
        }

        var existingDraft = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.Status == DraftStatus)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingDraft != null)
        {
            var correctedTitle = WithVersionSuffix(existingDraft.Title, existingDraft.VersionNumber);
            if (!existingDraft.Title.Equals(correctedTitle, StringComparison.Ordinal))
            {
                existingDraft.Title = correctedTitle;
                sourceVersion.Roadmap.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return await queryService.GetRoadmapDetailAsync(
                sourceVersion.RoadmapId,
                existingDraft.RoadmapVersionId,
                cancellationToken);
        }

        var currentMaxVersionNumber = await dbContext.Set<RoadmapVersion>()
            .Where(version => version.RoadmapId == sourceVersion.RoadmapId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var nextVersionNumber = currentMaxVersionNumber + 1;
        var baseTitle = ContentManagerRoadmapText.NormalizeOptionalText(request.Title) ?? sourceVersion.Title;

        var draftVersion = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = sourceVersion.RoadmapId,
            VersionNumber = nextVersionNumber,
            Status = DraftStatus,
            Title = WithVersionSuffix(baseTitle, nextVersionNumber),
            Description = sourceVersion.Description,
            EstimatedTotalHours = sourceVersion.EstimatedTotalHours,
            LayoutDirection = sourceVersion.LayoutDirection,
            LayoutAlgorithm = sourceVersion.LayoutAlgorithm,
            GeneratedByUserId = sourceVersion.GeneratedByUserId,
            GenerationPrompt = sourceVersion.GenerationPrompt,
            GenerationModel = sourceVersion.GenerationModel,
            GenerationStatus = sourceVersion.GenerationStatus,
            GenerationContext = sourceVersion.GenerationContext,
            GenerationError = null,
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Set<RoadmapVersion>().Add(draftVersion);

        var sourceNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == sourceVersion.RoadmapVersionId)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        var nodeIdMap = sourceNodes.ToDictionary(node => node.RoadmapNodeId, _ => Guid.NewGuid());

        foreach (var sourceNode in sourceNodes)
        {
            dbContext.Set<RoadmapNode>().Add(CloneNode(sourceNode, draftVersion.RoadmapVersionId, nodeIdMap));
        }

        await CloneEdgesAsync(sourceVersion.RoadmapVersionId, draftVersion.RoadmapVersionId, nodeIdMap, cancellationToken);
        await CloneSkillMappingsAsync(nodeIdMap, cancellationToken);
        await CloneResourceMappingsAsync(nodeIdMap, cancellationToken);

        sourceVersion.Roadmap.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(
            sourceVersion.RoadmapId,
            draftVersion.RoadmapVersionId,
            cancellationToken);
    }

    public async Task<ContentRoadmapDetailDto> PublishRoadmapVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var draftVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (draftVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        EnsureDraftVersion(draftVersion);

        var validation = await validationService.ValidateRoadmapVersionAsync(roadmapVersionId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("The draft has validation errors and cannot be published.");
        }

        var publishedVersions = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == draftVersion.RoadmapId
                && version.Status == PublishedStatus
                && version.RoadmapVersionId != draftVersion.RoadmapVersionId)
            .ToListAsync(cancellationToken);

        foreach (var publishedVersion in publishedVersions)
        {
            publishedVersion.Status = ArchivedStatus;
        }

        draftVersion.Status = PublishedStatus;
        draftVersion.PublishedAt = DateTime.UtcNow;
        draftVersion.Roadmap.Title = draftVersion.Title;
        draftVersion.Roadmap.Description = draftVersion.Description;
        draftVersion.Roadmap.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(
            draftVersion.RoadmapId,
            draftVersion.RoadmapVersionId,
            cancellationToken);
    }

    public async Task DeleteDraftVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var draftVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (draftVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        EnsureDraftVersion(draftVersion);

        var hasEnrollments = await dbContext.Set<RoadmapEnrollment>()
            .AnyAsync(enrollment => enrollment.RoadmapVersionId == roadmapVersionId, cancellationToken);
        if (hasEnrollments)
        {
            throw new InvalidOperationException("This draft cannot be deleted because it already has learner activity.");
        }

        var nodeIds = await dbContext.Set<RoadmapNode>()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .Select(node => node.RoadmapNodeId)
            .ToListAsync(cancellationToken);

        var nodeSkills = await dbContext.Set<RoadmapNodeSkill>()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var nodeResources = await dbContext.Set<RoadmapNodeResource>()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var edges = await dbContext.Set<RoadmapEdge>()
            .Where(edge => edge.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);
        var nodes = await dbContext.Set<RoadmapNode>()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        dbContext.Set<RoadmapNodeSkill>().RemoveRange(nodeSkills);
        dbContext.Set<RoadmapNodeResource>().RemoveRange(nodeResources);
        dbContext.Set<RoadmapEdge>().RemoveRange(edges);
        dbContext.Set<RoadmapNode>().RemoveRange(nodes);
        dbContext.Set<RoadmapVersion>().Remove(draftVersion);

        draftVersion.Roadmap.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static void EnsureDraftVersion(RoadmapVersion version)
    {
        if (!version.Status.Equals(DraftStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only draft roadmap versions can be structurally edited.");
        }
    }

    private static string WithVersionSuffix(string title, int versionNumber)
    {
        var baseTitle = ContentManagerRoadmapText.NormalizeRequiredText(title, "Roadmap title is required.");
        var withoutVersion = Regex.Replace(baseTitle, @"\s+v\d+\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        return $"{withoutVersion} v{versionNumber}";
    }

    private static RoadmapNode CloneNode(
        RoadmapNode sourceNode,
        Guid draftVersionId,
        IReadOnlyDictionary<Guid, Guid> nodeIdMap)
    {
        return new RoadmapNode
        {
            RoadmapNodeId = nodeIdMap[sourceNode.RoadmapNodeId],
            RoadmapVersionId = draftVersionId,
            ParentNodeId = sourceNode.ParentNodeId.HasValue && nodeIdMap.TryGetValue(sourceNode.ParentNodeId.Value, out var parentId)
                ? parentId
                : null,
            Slug = sourceNode.Slug,
            NodeType = sourceNode.NodeType,
            CheckpointType = sourceNode.CheckpointType,
            SelectionType = sourceNode.SelectionType,
            RequiredCount = sourceNode.RequiredCount,
            Title = sourceNode.Title,
            Description = sourceNode.Description,
            Reason = sourceNode.Reason,
            OrderIndex = sourceNode.OrderIndex,
            LayoutRole = sourceNode.LayoutRole,
            LayoutGroup = sourceNode.LayoutGroup,
            LayoutRank = sourceNode.LayoutRank,
            LayoutOrder = sourceNode.LayoutOrder,
            EstimatedHours = sourceNode.EstimatedHours,
            DifficultyLevel = sourceNode.DifficultyLevel,
            Priority = sourceNode.Priority,
            PositionX = sourceNode.PositionX,
            PositionY = sourceNode.PositionY,
            Metadata = sourceNode.Metadata,
            IsRequired = sourceNode.IsRequired,
            IsTrackable = sourceNode.IsTrackable,
            LearningOutcomes = sourceNode.LearningOutcomes,
            CompletionCriteria = sourceNode.CompletionCriteria,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task CloneEdgesAsync(
        Guid sourceVersionId,
        Guid draftVersionId,
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceEdges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        foreach (var sourceEdge in sourceEdges)
        {
            if (!nodeIdMap.TryGetValue(sourceEdge.FromNodeId, out var fromNodeId)
                || !nodeIdMap.TryGetValue(sourceEdge.ToNodeId, out var toNodeId))
            {
                continue;
            }

            dbContext.Set<RoadmapEdge>().Add(new RoadmapEdge
            {
                RoadmapEdgeId = Guid.NewGuid(),
                RoadmapVersionId = draftVersionId,
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId,
                EdgeType = sourceEdge.EdgeType,
                DependencyType = sourceEdge.DependencyType,
                Condition = sourceEdge.Condition
            });
        }
    }

    private async Task CloneSkillMappingsAsync(
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceNodeIds = nodeIdMap.Keys.ToList();
        var mappings = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Where(mapping => sourceNodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            dbContext.Set<RoadmapNodeSkill>().Add(new RoadmapNodeSkill
            {
                RoadmapNodeSkillId = Guid.NewGuid(),
                RoadmapNodeId = nodeIdMap[mapping.RoadmapNodeId],
                SkillId = mapping.SkillId
            });
        }
    }

    private async Task CloneResourceMappingsAsync(
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceNodeIds = nodeIdMap.Keys.ToList();
        var mappings = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Where(mapping => sourceNodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            dbContext.Set<RoadmapNodeResource>().Add(new RoadmapNodeResource
            {
                RoadmapNodeResourceId = Guid.NewGuid(),
                RoadmapNodeId = nodeIdMap[mapping.RoadmapNodeId],
                LearningResourceId = mapping.LearningResourceId,
                OrderIndex = mapping.OrderIndex,
                IsPrimary = mapping.IsPrimary
            });
        }
    }
}
