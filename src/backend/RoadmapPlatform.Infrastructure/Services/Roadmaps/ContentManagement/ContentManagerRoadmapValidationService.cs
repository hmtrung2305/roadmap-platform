using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapValidationService(ApplicationDbContext dbContext)
{
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

        var nodeIds = nodes.Select(node => node.RoadmapNodeId).ToHashSet();
        var childrenByParent = nodes
            .Where(node => node.ParentNodeId.HasValue)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var errors = new List<ContentRoadmapValidationItemDto>();
        var warnings = new List<ContentRoadmapValidationItemDto>();

        AddVersionValidation(version, nodes, errors);
        AddNodeValidation(nodes, nodeIds, childrenByParent, errors, warnings);
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
        if (string.IsNullOrWhiteSpace(version.Title))
        {
            errors.Add(CreateItem("roadmap_title_required", "Roadmap title is required."));
        }

        if (!nodes.Any(node => ContentManagerRoadmapStructureRules.NormalizeNodeType(node.NodeType) == "phase"))
        {
            errors.Add(CreateItem("phase_required", "Add at least one phase before publishing."));
        }
    }

    private static void AddNodeValidation(
        IReadOnlyCollection<RoadmapNode> nodes,
        HashSet<Guid> nodeIds,
        Dictionary<Guid, List<RoadmapNode>> childrenByParent,
        List<ContentRoadmapValidationItemDto> errors,
        List<ContentRoadmapValidationItemDto> warnings)
    {
        foreach (var node in nodes)
        {
            var nodeType = ContentManagerRoadmapStructureRules.NormalizeNodeType(node.NodeType);

            if (string.IsNullOrWhiteSpace(node.Title))
            {
                errors.Add(CreateItem("node_title_required", "Node title is required.", node));
            }

            if (node.ParentNodeId.HasValue && !nodeIds.Contains(node.ParentNodeId.Value))
            {
                errors.Add(CreateItem("node_parent_missing", "Node parent does not exist in this roadmap version.", node));
            }

            if (nodeType == "phase" && node.ParentNodeId.HasValue)
            {
                errors.Add(CreateItem("phase_parent_invalid", "Phases must stay at the top level.", node));
            }

            if (nodeType is "resource_group" or "group" && !HasParentOfType(node, nodes, "phase"))
            {
                errors.Add(CreateItem("group_parent_invalid", "Groups must belong to a phase.", node));
            }

            if (nodeType is "topic" or "project" or "checkpoint")
            {
                if (!HasParentOfType(node, nodes, "phase", "resource_group", "group", "choice_group"))
                {
                    errors.Add(CreateItem("learning_node_parent_invalid", "Learning nodes must belong to a phase or group.", node));
                }

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

            if (nodeType is "phase" or "resource_group" or "group" or "choice_group")
            {
                var children = childrenByParent.GetValueOrDefault(node.RoadmapNodeId) ?? [];
                if (children.Count == 0)
                {
                    warnings.Add(CreateItem("container_empty", "This container has no child nodes.", node));
                }
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

    private static bool HasParentOfType(RoadmapNode node, IEnumerable<RoadmapNode> nodes, params string[] allowedParentTypes)
    {
        if (!node.ParentNodeId.HasValue)
        {
            return false;
        }

        var parent = nodes.FirstOrDefault(item => item.RoadmapNodeId == node.ParentNodeId.Value);
        if (parent == null)
        {
            return false;
        }

        var normalizedParentType = ContentManagerRoadmapStructureRules.NormalizeNodeType(parent.NodeType);
        return allowedParentTypes.Any(type => normalizedParentType.Equals(type, StringComparison.OrdinalIgnoreCase));
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
