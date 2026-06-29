using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapValidationService(ApplicationDbContext dbContext)
{
    private static readonly string[] LearnerFacingNodeTypes = ["topic", "project", "checkpoint", "choice_option"];
    private static readonly string[] GroupNodeTypes = ["choice_group", "resource_group", "group"];

    public async Task<ContentRoadmapValidationResultDto> ValidateRoadmapVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var version = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Include(item => item.Roadmap)
            .Where(item => item.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        var nodesById = nodes.ToDictionary(node => node.RoadmapNodeId);
        var nodeIds = nodesById.Keys.ToHashSet();
        var childrenByParent = nodes
            .Where(node => node.ParentNodeId.HasValue)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var errors = new List<ContentRoadmapValidationItemDto>();
        var warnings = new List<ContentRoadmapValidationItemDto>();

        AddVersionValidation(version, nodes, errors);
        AddNodeValidation(nodes, nodesById, nodeIds, childrenByParent, errors, warnings);
        AddSiblingOrderValidation(nodes, errors);
        await AddMappingWarningsAsync(nodes, warnings, cancellationToken);

        return new ContentRoadmapValidationResultDto
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void AddVersionValidation(
        RoadmapVersion version,
        IReadOnlyCollection<RoadmapNode> nodes,
        List<ContentRoadmapValidationItemDto> errors)
    {
        if (!version.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(CreateItem("version_must_be_draft", "Only draft roadmap versions can be published."));
        }

        if (string.IsNullOrWhiteSpace(version.Title))
        {
            errors.Add(CreateItem("roadmap_title_required", "Roadmap title is required."));
        }

        if (version.Roadmap == null || version.Roadmap.CareerRoleId == Guid.Empty)
        {
            errors.Add(CreateItem("career_role_required", "Career role is required."));
        }

        if (!nodes.Any(node => IsNodeType(node, "phase")))
        {
            errors.Add(CreateItem("phase_required", "Add at least one phase before publishing."));
        }

        if (!nodes.Any(node => LearnerFacingNodeTypes.Contains(NormalizeNodeType(node), StringComparer.OrdinalIgnoreCase)))
        {
            errors.Add(CreateItem("learner_node_required", "Add at least one learner-facing node before publishing."));
        }
    }

    private static void AddNodeValidation(
        IReadOnlyCollection<RoadmapNode> nodes,
        IReadOnlyDictionary<Guid, RoadmapNode> nodesById,
        HashSet<Guid> nodeIds,
        Dictionary<Guid, List<RoadmapNode>> childrenByParent,
        List<ContentRoadmapValidationItemDto> errors,
        List<ContentRoadmapValidationItemDto> warnings)
    {
        foreach (var node in nodes)
        {
            var nodeType = NormalizeNodeType(node);

            if (string.IsNullOrWhiteSpace(node.Title))
            {
                errors.Add(CreateItem("node_title_required", "Node title is required.", node));
            }

            if (node.ParentNodeId.HasValue && !nodeIds.Contains(node.ParentNodeId.Value))
            {
                errors.Add(CreateItem("node_parent_missing", "Node parent does not exist in this roadmap version.", node));
            }

            if (HasCircularParentChain(node, nodesById))
            {
                errors.Add(CreateItem("node_parent_cycle", "Node parent relationships cannot be circular.", node));
            }

            if (nodeType == "phase" && node.ParentNodeId.HasValue)
            {
                errors.Add(CreateItem("phase_parent_invalid", "Phase nodes must be top-level.", node));
            }

            if (GroupNodeTypes.Contains(nodeType, StringComparer.OrdinalIgnoreCase) && !HasDirectParentOfType(node, nodesById, "phase"))
            {
                errors.Add(CreateItem("group_parent_invalid", "Group nodes must belong directly to a phase.", node));
            }

            if (LearnerFacingNodeTypes.Contains(nodeType, StringComparer.OrdinalIgnoreCase)
                && !HasDirectParentOfType(node, nodesById, "phase", "resource_group", "group", "choice_group"))
            {
                errors.Add(CreateItem("learning_node_parent_invalid", "Learner-facing nodes must belong to a phase or group.", node));
            }

            if (ContentManagerRoadmapStructureRules.IsLeafNodeType(nodeType)
                && childrenByParent.TryGetValue(node.RoadmapNodeId, out var children)
                && children.Count > 0)
            {
                errors.Add(CreateItem("leaf_node_has_children", "Leaf nodes cannot have child nodes.", node));
            }

            if (LearnerFacingNodeTypes.Contains(nodeType, StringComparer.OrdinalIgnoreCase))
            {
                AddLearnerNodeWarnings(node, warnings);
            }

            if (ContentManagerRoadmapStructureRules.IsContainerNode(nodeType))
            {
                var containerChildren = childrenByParent.GetValueOrDefault(node.RoadmapNodeId) ?? [];
                if (containerChildren.Count == 0)
                {
                    warnings.Add(CreateItem("container_empty", "This container has no child nodes.", node));
                }
            }
        }
    }

    private static void AddLearnerNodeWarnings(
        RoadmapNode node,
        List<ContentRoadmapValidationItemDto> warnings)
    {
        if (string.IsNullOrWhiteSpace(node.Description))
        {
            warnings.Add(CreateItem("node_description_missing", "Description is missing.", node));
        }

        if (ContentManagerRoadmapNodeContent.DeserializeStringArray(node.LearningOutcomes).Count == 0)
        {
            warnings.Add(CreateItem("learning_outcomes_missing", "Learning outcomes are missing.", node));
        }

        if (ContentManagerRoadmapNodeContent.DeserializeStringArray(node.CompletionCriteria).Count == 0)
        {
            warnings.Add(CreateItem("completion_criteria_missing", "Completion criteria are missing.", node));
        }
    }

    private static void AddSiblingOrderValidation(
        IReadOnlyCollection<RoadmapNode> nodes,
        List<ContentRoadmapValidationItemDto> errors)
    {
        foreach (var siblingGroup in nodes.GroupBy(node => node.ParentNodeId))
        {
            foreach (var node in siblingGroup.Where(node => node.OrderIndex <= 0))
            {
                errors.Add(CreateItem("node_order_invalid", "Sibling order index values must be greater than zero.", node));
            }

            var duplicateOrderNodes = siblingGroup
                .Where(node => node.OrderIndex > 0)
                .GroupBy(node => node.OrderIndex)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList();

            foreach (var node in duplicateOrderNodes)
            {
                errors.Add(CreateItem("node_order_duplicate", "Sibling order index values must be unique.", node));
            }
        }
    }

    private async Task AddMappingWarningsAsync(
        IReadOnlyCollection<RoadmapNode> nodes,
        List<ContentRoadmapValidationItemDto> warnings,
        CancellationToken cancellationToken)
    {
        var mappableNodes = nodes
            .Where(node => ContentManagerRoadmapNodeRules.CanHaveMappings(node.NodeType))
            .ToList();

        if (mappableNodes.Count == 0)
        {
            return;
        }

        var nodeIds = mappableNodes.Select(node => node.RoadmapNodeId).ToList();
        var nodesWithSkills = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .Select(mapping => mapping.RoadmapNodeId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var nodesWithResources = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .Select(mapping => mapping.RoadmapNodeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var skillSet = nodesWithSkills.ToHashSet();
        var resourceSet = nodesWithResources.ToHashSet();

        foreach (var node in mappableNodes)
        {
            if (!skillSet.Contains(node.RoadmapNodeId))
            {
                warnings.Add(CreateItem("skills_missing", "No skills are mapped.", node));
            }

            if (!resourceSet.Contains(node.RoadmapNodeId))
            {
                warnings.Add(CreateItem("resources_missing", "No resources are mapped.", node));
            }
        }
    }

    private static bool IsNodeType(RoadmapNode node, string nodeType)
    {
        return NormalizeNodeType(node).Equals(nodeType, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeNodeType(RoadmapNode node)
    {
        return ContentManagerRoadmapStructureRules.NormalizeNodeType(node.NodeType);
    }

    private static bool HasDirectParentOfType(
        RoadmapNode node,
        IReadOnlyDictionary<Guid, RoadmapNode> nodesById,
        params string[] allowedParentTypes)
    {
        if (!node.ParentNodeId.HasValue || !nodesById.TryGetValue(node.ParentNodeId.Value, out var parent))
        {
            return false;
        }

        var normalizedParentType = NormalizeNodeType(parent);
        return allowedParentTypes.Any(type => normalizedParentType.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCircularParentChain(
        RoadmapNode node,
        IReadOnlyDictionary<Guid, RoadmapNode> nodesById)
    {
        var seenNodeIds = new HashSet<Guid> { node.RoadmapNodeId };
        var current = node;

        while (current.ParentNodeId.HasValue)
        {
            var parentNodeId = current.ParentNodeId.Value;
            if (!seenNodeIds.Add(parentNodeId))
            {
                return true;
            }

            if (!nodesById.TryGetValue(parentNodeId, out var parent))
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    private static ContentRoadmapValidationItemDto CreateItem(
        string code,
        string message,
        RoadmapNode? node = null)
    {
        return new ContentRoadmapValidationItemDto
        {
            Code = code,
            Message = message,
            RoadmapNodeId = node?.RoadmapNodeId,
            NodeTitle = node?.Title
        };
    }
}
