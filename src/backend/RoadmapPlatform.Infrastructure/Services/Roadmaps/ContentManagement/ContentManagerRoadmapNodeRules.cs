using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapNodeRules
{
    private static readonly HashSet<string> ManualLearningNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "topic",
        "choice_option",
        "checkpoint",
        "project"
    };

    private static readonly HashSet<string> MappingNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "topic",
        "choice_option",
        "project"
    };

    public static bool CanHaveLearningMetadata(string? nodeType)
    {
        return !string.IsNullOrWhiteSpace(nodeType) && ManualLearningNodeTypes.Contains(nodeType);
    }

    public static bool CanHaveMappings(string? nodeType)
    {
        return !string.IsNullOrWhiteSpace(nodeType) && MappingNodeTypes.Contains(nodeType);
    }

    public static void EnsureNodeSupportsMappings(RoadmapNode node)
    {
        if (!CanHaveMappings(node.NodeType))
        {
            throw new ArgumentException("Only topic, choice option, and project nodes can have skill and resource mappings.");
        }
    }
}
