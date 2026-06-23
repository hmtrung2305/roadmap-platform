using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

internal static class ContentRoadmapNodeRules
{
    private static readonly HashSet<string> LearningMetadataNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "topic",
        "project"
    };

    public static bool CanHaveLearningMetadata(string? nodeType)
    {
        return !string.IsNullOrWhiteSpace(nodeType) && LearningMetadataNodeTypes.Contains(nodeType);
    }

    public static void EnsureNodeSupportsMappings(RoadmapNode node)
    {
        if (!CanHaveLearningMetadata(node.NodeType))
        {
            throw new ArgumentException("Only topic and project nodes can have skill and resource mappings.");
        }
    }
}
