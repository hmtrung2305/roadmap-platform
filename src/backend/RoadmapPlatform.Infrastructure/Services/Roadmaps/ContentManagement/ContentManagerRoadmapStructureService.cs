using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapStructureService(
    ApplicationDbContext dbContext,
    ContentManagerRoadmapQueryService queryService)
{
    public async Task<ContentRoadmapStructureMutationResultDto> CreateNodeAsync(
        Guid roadmapVersionId,
        CreateRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var version = await LoadVersionForMutationAsync(roadmapVersionId, cancellationToken);
        var title = ContentManagerRoadmapText.NormalizeRequiredText(request.Title, "Node title is required.");
        var nodeType = ContentManagerRoadmapStructureRules.NormalizeNodeType(request.NodeType);
        ContentManagerRoadmapStructureRules.EnsureSupportedNodeType(nodeType);

        var parentNode = await LoadParentNodeAsync(roadmapVersionId, request.ParentNodeId, cancellationToken);
        ContentManagerRoadmapStructureRules.ValidateCreateRequest(request, nodeType, parentNode);

        var siblings = await LoadSiblingsAsync(roadmapVersionId, parentNode?.RoadmapNodeId, cancellationToken);
        var insertionIndex = GetInsertionIndex(siblings, request.Position, request.ReferenceNodeId);
        ShiftSiblingsForInsert(siblings, insertionIndex);
        var isRequired = ContentManagerRoadmapStructureRules.GetIsRequiredForCreate(nodeType, request.IsRequired);

        var node = new RoadmapNode
        {
            RoadmapNodeId = Guid.NewGuid(),
            RoadmapVersionId = roadmapVersionId,
            ParentNodeId = parentNode?.RoadmapNodeId,
            Slug = await CreateUniqueSlugAsync(roadmapVersionId, title, cancellationToken),
            NodeType = nodeType,
            CheckpointType = ContentManagerRoadmapStructureRules.GetDefaultCheckpointType(nodeType, request.CheckpointType),
            SelectionType = null,
            RequiredCount = null,
            Title = title,
            Description = ContentManagerRoadmapText.NormalizeOptionalText(request.Description),
            OrderIndex = insertionIndex,
            LayoutRole = ContentManagerRoadmapStructureRules.GetLayoutRoleForCreate(nodeType, parentNode),
            EstimatedHours = ContentManagerRoadmapNodeRules.CanHaveLearningMetadata(nodeType) ? request.EstimatedHours : null,
            DifficultyLevel = ContentManagerRoadmapNodeRules.CanHaveLearningMetadata(nodeType)
                ? ContentManagerRoadmapText.NormalizeOptionalText(request.DifficultyLevel)?.ToLowerInvariant()
                : null,
            Metadata = "{}",
            IsRequired = isRequired,
            IsTrackable = ContentManagerRoadmapStructureRules.GetDefaultIsTrackable(nodeType),
            LearningOutcomes = "[]",
            CompletionCriteria = "[]",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Set<RoadmapNode>().Add(node);
        AddParentEdge(node, parentNode);

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateLayoutAsync(roadmapVersionId, cancellationToken);
        TouchVersion(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContentRoadmapStructureMutationResultDto
        {
            Roadmap = await queryService.GetRoadmapDetailAsync(version.RoadmapId, roadmapVersionId, cancellationToken),
            FocusNodeId = node.RoadmapNodeId
        };
    }

    public async Task<ContentRoadmapStructureMutationResultDto> MoveNodeAsync(
        Guid roadmapNodeId,
        MoveRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        var node = await LoadNodeForMutationAsync(roadmapNodeId, cancellationToken);
        var version = await LoadVersionForMutationAsync(node.RoadmapVersionId, cancellationToken);
        var direction = ContentManagerRoadmapText.NormalizeOptionalText(request.Direction)?.ToLowerInvariant();
        if (direction is not ("up" or "down"))
        {
            throw new ArgumentException("Move direction must be up or down.");
        }

        var siblings = await LoadSiblingsAsync(node.RoadmapVersionId, node.ParentNodeId, cancellationToken);
        var ordered = siblings.OrderBy(item => item.OrderIndex).ThenBy(item => item.Title).ToList();
        var index = ordered.FindIndex(item => item.RoadmapNodeId == roadmapNodeId);
        var swapIndex = direction == "up" ? index - 1 : index + 1;

        if (index < 0 || swapIndex < 0 || swapIndex >= ordered.Count)
        {
            return new ContentRoadmapStructureMutationResultDto
            {
                Roadmap = await queryService.GetRoadmapDetailAsync(version.RoadmapId, node.RoadmapVersionId, cancellationToken),
                FocusNodeId = roadmapNodeId
            };
        }

        (ordered[index].OrderIndex, ordered[swapIndex].OrderIndex) = (ordered[swapIndex].OrderIndex, ordered[index].OrderIndex);

        await NormalizeSiblingOrdersAsync(node.RoadmapVersionId, node.ParentNodeId, cancellationToken);
        await RecalculateLayoutAsync(node.RoadmapVersionId, cancellationToken);
        TouchVersion(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContentRoadmapStructureMutationResultDto
        {
            Roadmap = await queryService.GetRoadmapDetailAsync(version.RoadmapId, node.RoadmapVersionId, cancellationToken),
            FocusNodeId = roadmapNodeId
        };
    }

    public async Task<ContentRoadmapStructureMutationResultDto> DeleteNodeAsync(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        var node = await LoadNodeForMutationAsync(roadmapNodeId, cancellationToken);
        var version = await LoadVersionForMutationAsync(node.RoadmapVersionId, cancellationToken);
        var nodeIdsToDelete = await GetNodeAndDescendantIdsAsync(node, cancellationToken);

        var nextFocusNode = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(item =>
                item.RoadmapVersionId == node.RoadmapVersionId
                && item.ParentNodeId == node.ParentNodeId
                && item.RoadmapNodeId != node.RoadmapNodeId
                && !nodeIdsToDelete.Contains(item.RoadmapNodeId))
            .OrderBy(item => Math.Abs(item.OrderIndex - node.OrderIndex))
            .Select(item => (Guid?)item.RoadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        nextFocusNode ??= await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(item =>
                item.RoadmapVersionId == node.RoadmapVersionId
                && item.RoadmapNodeId == node.ParentNodeId
                && !nodeIdsToDelete.Contains(item.RoadmapNodeId))
            .Select(item => (Guid?)item.RoadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        var edges = await dbContext.Set<RoadmapEdge>()
            .Where(edge =>
                edge.RoadmapVersionId == node.RoadmapVersionId
                && (nodeIdsToDelete.Contains(edge.FromNodeId) || nodeIdsToDelete.Contains(edge.ToNodeId)))
            .ToListAsync(cancellationToken);
        var skills = await dbContext.Set<RoadmapNodeSkill>()
            .Where(mapping => nodeIdsToDelete.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var resources = await dbContext.Set<RoadmapNodeResource>()
            .Where(mapping => nodeIdsToDelete.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var nodes = await dbContext.Set<RoadmapNode>()
            .Where(item => nodeIdsToDelete.Contains(item.RoadmapNodeId))
            .OrderByDescending(item => item.ParentNodeId.HasValue)
            .ToListAsync(cancellationToken);

        dbContext.Set<RoadmapEdge>().RemoveRange(edges);
        dbContext.Set<RoadmapNodeSkill>().RemoveRange(skills);
        dbContext.Set<RoadmapNodeResource>().RemoveRange(resources);
        dbContext.Set<RoadmapNode>().RemoveRange(nodes);

        await dbContext.SaveChangesAsync(cancellationToken);
        await NormalizeSiblingOrdersAsync(node.RoadmapVersionId, node.ParentNodeId, cancellationToken);
        await RecalculateLayoutAsync(node.RoadmapVersionId, cancellationToken);
        TouchVersion(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContentRoadmapStructureMutationResultDto
        {
            Roadmap = await queryService.GetRoadmapDetailAsync(version.RoadmapId, node.RoadmapVersionId, cancellationToken),
            FocusNodeId = nextFocusNode
        };
    }

    private async Task<HashSet<Guid>> GetNodeAndDescendantIdsAsync(
        RoadmapNode rootNode,
        CancellationToken cancellationToken)
    {
        var allNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(item => item.RoadmapVersionId == rootNode.RoadmapVersionId)
            .Select(item => new { item.RoadmapNodeId, item.ParentNodeId })
            .ToListAsync(cancellationToken);

        var childrenByParent = allNodes
            .Where(item => item.ParentNodeId.HasValue)
            .GroupBy(item => item.ParentNodeId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.RoadmapNodeId).ToList());

        var ids = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootNode.RoadmapNodeId);

        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            if (!ids.Add(nodeId))
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(nodeId, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                queue.Enqueue(childId);
            }
        }

        return ids;
    }

    private static void TouchVersion(RoadmapVersion version)
    {
        version.UpdatedAt = DateTime.UtcNow;
        version.Roadmap.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<RoadmapVersion> LoadVersionForMutationAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.Set<RoadmapVersion>()
            .Include(item => item.Roadmap)
            .Where(item => item.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        ContentManagerRoadmapDraftService.EnsureStructuralDraftVersion(version);
        return version;
    }

    private async Task<RoadmapNode> LoadNodeForMutationAsync(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        if (roadmapNodeId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap node was not provided.", nameof(roadmapNodeId));
        }

        var node = await dbContext.Set<RoadmapNode>()
            .Where(item => item.RoadmapNodeId == roadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        return node ?? throw new KeyNotFoundException("Roadmap node was not found.");
    }

    private async Task<RoadmapNode?> LoadParentNodeAsync(
        Guid roadmapVersionId,
        Guid? parentNodeId,
        CancellationToken cancellationToken)
    {
        if (!parentNodeId.HasValue)
        {
            return null;
        }

        var parentNode = await dbContext.Set<RoadmapNode>()
            .Where(item => item.RoadmapVersionId == roadmapVersionId && item.RoadmapNodeId == parentNodeId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return parentNode ?? throw new KeyNotFoundException("Parent node was not found.");
    }

    private async Task<List<RoadmapNode>> LoadSiblingsAsync(
        Guid roadmapVersionId,
        Guid? parentNodeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<RoadmapNode>()
            .Where(item =>
                item.RoadmapVersionId == roadmapVersionId
                && item.ParentNodeId == parentNodeId)
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
    }

    private static int GetInsertionIndex(
        IReadOnlyList<RoadmapNode> siblings,
        string? requestedPosition,
        Guid? referenceNodeId)
    {
        var position = ContentManagerRoadmapStructureRules.NormalizePosition(requestedPosition);
        if (position == "end" || !referenceNodeId.HasValue)
        {
            return siblings.Count + 1;
        }

        var referenceIndex = siblings.ToList().FindIndex(item => item.RoadmapNodeId == referenceNodeId.Value);
        if (referenceIndex < 0)
        {
            return siblings.Count + 1;
        }

        return position == "before" ? referenceIndex + 1 : referenceIndex + 2;
    }

    private static void ShiftSiblingsForInsert(IEnumerable<RoadmapNode> siblings, int insertionIndex)
    {
        foreach (var sibling in siblings.Where(item => item.OrderIndex >= insertionIndex))
        {
            sibling.OrderIndex += 1;
        }
    }

    private async Task NormalizeSiblingOrdersAsync(
        Guid roadmapVersionId,
        Guid? parentNodeId,
        CancellationToken cancellationToken)
    {
        var siblings = await LoadSiblingsAsync(roadmapVersionId, parentNodeId, cancellationToken);
        var order = 1;
        foreach (var sibling in siblings.OrderBy(item => item.OrderIndex).ThenBy(item => item.Title))
        {
            sibling.OrderIndex = order++;
        }
    }

    private void AddParentEdge(RoadmapNode node, RoadmapNode? parentNode)
    {
        if (parentNode == null)
        {
            return;
        }

        dbContext.Set<RoadmapEdge>().Add(new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = node.RoadmapVersionId,
            FromNodeId = parentNode.RoadmapNodeId,
            ToNodeId = node.RoadmapNodeId,
            EdgeType = ContentManagerRoadmapStructureRules.NormalizeNodeType(parentNode.NodeType) == "choice_group" ? "choice" : "contains",
            DependencyType = "required",
            Condition = "{}"
        });
    }

    private async Task<string> CreateUniqueSlugAsync(
        Guid roadmapVersionId,
        string title,
        CancellationToken cancellationToken)
    {
        var baseSlug = ContentManagerRoadmapText.Slugify(title);
        var existingSlugs = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == roadmapVersionId && node.Slug.StartsWith(baseSlug))
            .Select(node => node.Slug)
            .ToListAsync(cancellationToken);

        if (!existingSlugs.Contains(baseSlug, StringComparer.OrdinalIgnoreCase))
        {
            return baseSlug;
        }

        var suffix = 2;
        while (existingSlugs.Contains($"{baseSlug}-{suffix}", StringComparer.OrdinalIgnoreCase))
        {
            suffix += 1;
        }

        return $"{baseSlug}-{suffix}";
    }

    private async Task RecalculateLayoutAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var nodes = await dbContext.Set<RoadmapNode>()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        var childrenByParent = nodes
            .Where(node => node.ParentNodeId.HasValue)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.OrderIndex).ToList());
        var phases = nodes
            .Where(ContentManagerRoadmapStructureRules.IsPhase)
            .OrderBy(node => node.OrderIndex)
            .ThenBy(node => node.Title)
            .ToList();

        for (var phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
        {
            var phase = phases[phaseIndex];
            phase.OrderIndex = phaseIndex + 1;
            phase.LayoutRole = "trunk";
            phase.IsTrackable = false;
        }

        foreach (var phase in phases)
        {
            ApplyLayoutToChildren(phase, childrenByParent);
        }

        await RebuildPhaseSequenceEdgesAsync(roadmapVersionId, phases, cancellationToken);
    }

    private static void ApplyLayoutToChildren(
        RoadmapNode parent,
        Dictionary<Guid, List<RoadmapNode>> childrenByParent)
    {
        var children = childrenByParent.GetValueOrDefault(parent.RoadmapNodeId) ?? [];
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var childType = ContentManagerRoadmapStructureRules.NormalizeNodeType(child.NodeType);
            child.OrderIndex = index + 1;
            child.LayoutRole = ContentManagerRoadmapStructureRules.GetPersistedLayoutRole(childType, child.LayoutRole);
            child.IsTrackable = ContentManagerRoadmapStructureRules.GetDefaultIsTrackable(childType) || child.IsTrackable;

            ApplyLayoutToChildren(child, childrenByParent);
        }
    }

    private async Task RebuildPhaseSequenceEdgesAsync(
        Guid roadmapVersionId,
        IReadOnlyList<RoadmapNode> phases,
        CancellationToken cancellationToken)
    {
        var existingPhaseSequenceEdges = await dbContext.Set<RoadmapEdge>()
            .Where(edge =>
                edge.RoadmapVersionId == roadmapVersionId
                && edge.EdgeType == "sequence")
            .ToListAsync(cancellationToken);

        dbContext.Set<RoadmapEdge>().RemoveRange(existingPhaseSequenceEdges);

        for (var index = 0; index < phases.Count - 1; index++)
        {
            dbContext.Set<RoadmapEdge>().Add(new RoadmapEdge
            {
                RoadmapEdgeId = Guid.NewGuid(),
                RoadmapVersionId = roadmapVersionId,
                FromNodeId = phases[index].RoadmapNodeId,
                ToNodeId = phases[index + 1].RoadmapNodeId,
                EdgeType = "sequence",
                DependencyType = "required",
                Condition = "{}"
            });
        }
    }
}
