using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapStructureRules
{
    private static readonly HashSet<string> SupportedCreatedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "phase",
        "resource_group",
        "group",
        "topic",
        "project",
        "checkpoint"
    };

    public static string NormalizeNodeType(string? nodeType)
    {
        var normalized = ContentManagerRoadmapText.NormalizeOptionalText(nodeType)?.ToLowerInvariant();
        if (normalized == "group")
        {
            return "resource_group";
        }

        return normalized ?? string.Empty;
    }

    public static string NormalizePosition(string? position)
    {
        var normalized = ContentManagerRoadmapText.NormalizeOptionalText(position)?.ToLowerInvariant();
        return normalized is "before" or "after" ? normalized : "end";
    }

    public static void EnsureSupportedNodeType(string nodeType)
    {
        if (!SupportedCreatedNodeTypes.Contains(nodeType))
        {
            throw new ArgumentException("Unsupported node type.");
        }
    }

    public static void ValidateCreateRequest(
        CreateRoadmapNodeRequestDto request,
        string normalizedNodeType,
        RoadmapNode? parentNode)
    {
        if (normalizedNodeType == "phase")
        {
            if (request.ParentNodeId.HasValue)
            {
                throw new ArgumentException("A phase cannot be added inside another node.");
            }

            return;
        }

        if (parentNode == null)
        {
            throw new ArgumentException("Select a parent node.");
        }

        var parentType = NormalizeNodeType(parentNode.NodeType);
        if (IsLeafNodeType(parentType))
        {
            throw new ArgumentException("This node type cannot contain child nodes.");
        }

        if (normalizedNodeType == "resource_group")
        {
            if (parentType != "phase")
            {
                throw new ArgumentException("A group must be added inside a phase.");
            }

            return;
        }

        if (normalizedNodeType is "topic" or "project" or "checkpoint")
        {
            if (parentType is not ("resource_group" or "choice_group"))
            {
                throw new ArgumentException("A learning node must be added inside a group.");
            }
        }
    }

    public static bool IsContainerNode(string? nodeType)
    {
        var normalized = NormalizeNodeType(nodeType);
        return normalized is "phase" or "resource_group" or "choice_group";
    }

    public static bool IsLeafNodeType(string? nodeType)
    {
        var normalized = NormalizeNodeType(nodeType);
        return normalized is "topic" or "choice_option" or "checkpoint" or "project";
    }

    public static bool IsPhase(RoadmapNode node)
    {
        return NormalizeNodeType(node.NodeType) == "phase";
    }

    public static bool IsTopLevelNode(RoadmapNode node)
    {
        return IsPhase(node) || !node.ParentNodeId.HasValue;
    }

    public static string GetDefaultLayoutRole(string nodeType)
    {
        return nodeType switch
        {
            "phase" => "trunk",
            "checkpoint" => "checkpoint",
            "project" => "required_project",
            _ => "side"
        };
    }

    public static bool GetDefaultIsTrackable(string nodeType)
    {
        return nodeType is "topic" or "project" or "checkpoint" or "choice_option";
    }

    public static string? GetDefaultCheckpointType(string nodeType, string? requestedCheckpointType)
    {
        if (nodeType != "checkpoint")
        {
            return null;
        }

        return ContentManagerRoadmapText.NormalizeOptionalText(requestedCheckpointType)?.ToLowerInvariant() ?? "review";
    }
}
