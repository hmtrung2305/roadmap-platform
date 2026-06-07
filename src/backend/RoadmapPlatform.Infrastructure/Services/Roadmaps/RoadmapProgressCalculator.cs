using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

internal sealed class RoadmapProgressCalculationResult
{
    public int TotalUnits { get; init; }
    public int CompletedUnits { get; init; }
    public decimal ProgressPercent { get; init; }
}

internal static class RoadmapProgressCalculator
{
    private static readonly HashSet<string> ManualNodeTypes =
    [
        "topic",
        "choice_option",
        "checkpoint",
        "project"
    ];

    private static readonly HashSet<string> ComputedNodeTypes =
    [
        "phase",
        "choice_group",
        "resource_group"
    ];

    private static readonly HashSet<string> LockingEdgeTypes =
    [
        "sequence",
        "dependency",
        "unlock"
    ];

    public static bool IsManualProgressNode(RoadmapNode node)
    {
        return node.IsTrackable && ManualNodeTypes.Contains(node.NodeType);
    }

    public static bool IsComputedNode(RoadmapNode node)
    {
        return ComputedNodeTypes.Contains(node.NodeType);
    }

    public static IReadOnlyDictionary<Guid, string> CalculateStatuses(
        IReadOnlyList<RoadmapNode> nodes,
        IReadOnlyList<RoadmapEdge> edges,
        IReadOnlyDictionary<Guid, UserNodeProgress> progressByNodeId)
    {
        var nodeById = nodes.ToDictionary(n => n.RoadmapNodeId);
        var statusByNodeId = new Dictionary<Guid, string>();
        var lockCache = new Dictionary<Guid, bool>();
        var visiting = new HashSet<Guid>();

        foreach (var node in nodes)
        {
            GetStatus(node.RoadmapNodeId);
        }

        return statusByNodeId;

        string GetStatus(Guid nodeId)
        {
            if (statusByNodeId.TryGetValue(nodeId, out var cachedStatus))
            {
                return cachedStatus;
            }

            if (!nodeById.TryGetValue(nodeId, out var node))
            {
                return "pending";
            }

            if (!visiting.Add(nodeId))
            {
                return "pending";
            }

            string status;

            if (IsLocked(nodeId))
            {
                status = "locked";
            }
            else if (node.NodeType == "choice_group")
            {
                status = CalculateChoiceGroupStatus(node);
            }
            else if (node.NodeType is "phase" or "resource_group")
            {
                status = CalculateContainerStatus(node);
            }
            else
            {
                status = progressByNodeId.TryGetValue(nodeId, out var progress)
                    ? progress.Status
                    : "pending";
            }

            visiting.Remove(nodeId);
            statusByNodeId[nodeId] = status;
            return status;
        }

        bool IsLocked(Guid nodeId)
        {
            if (lockCache.TryGetValue(nodeId, out var cachedLocked))
            {
                return cachedLocked;
            }

            if (!nodeById.TryGetValue(nodeId, out var node))
            {
                lockCache[nodeId] = false;
                return false;
            }

            if (node.ParentNodeId.HasValue && IsLocked(node.ParentNodeId.Value))
            {
                lockCache[nodeId] = true;
                return true;
            }

            var requiredPrerequisites = edges
                .Where(e =>
                    e.ToNodeId == nodeId &&
                    e.DependencyType == "required" &&
                    LockingEdgeTypes.Contains(e.EdgeType))
                .Select(e => e.FromNodeId)
                .Distinct()
                .ToList();

            if (requiredPrerequisites.Count == 0)
            {
                lockCache[nodeId] = false;
                return false;
            }

            var locked = requiredPrerequisites.Any(id => GetStatus(id) != "completed");
            lockCache[nodeId] = locked;
            return locked;
        }

        string CalculateContainerStatus(RoadmapNode node)
        {
            var children = GetRequiredChildren(node).ToList();

            if (children.Count == 0)
            {
                return progressByNodeId.TryGetValue(node.RoadmapNodeId, out var progress)
                    ? progress.Status
                    : "pending";
            }

            var childStatuses = children.Select(child => GetStatus(child.RoadmapNodeId)).ToList();

            if (childStatuses.All(status => status == "completed" || status == "skipped"))
            {
                return "completed";
            }

            if (childStatuses.Any(status => status is "completed" or "in_progress" or "skipped"))
            {
                return "in_progress";
            }

            return "pending";
        }

        string CalculateChoiceGroupStatus(RoadmapNode node)
        {
            var children = GetRequiredChildren(node).ToList();

            if (children.Count == 0)
            {
                return "pending";
            }

            var childStatuses = children.Select(child => GetStatus(child.RoadmapNodeId)).ToList();
            var completedCount = childStatuses.Count(status => status == "completed" || status == "skipped");
            var requiredCount = GetRequiredCount(node, children.Count);

            if (completedCount >= requiredCount)
            {
                return "completed";
            }

            if (childStatuses.Any(status => status is "completed" or "in_progress" or "skipped"))
            {
                return "in_progress";
            }

            return "pending";
        }

        IEnumerable<RoadmapNode> GetRequiredChildren(RoadmapNode node)
        {
            var childIds = edges
                .Where(e =>
                    e.FromNodeId == node.RoadmapNodeId &&
                    e.DependencyType == "required" &&
                    e.EdgeType is "contains" or "choice")
                .Select(e => e.ToNodeId)
                .ToHashSet();

            var children = childIds.Count > 0
                ? nodes.Where(n => childIds.Contains(n.RoadmapNodeId))
                : nodes.Where(n => n.ParentNodeId == node.RoadmapNodeId);

            return children
                .Where(n => n.IsRequired)
                .OrderBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex);
        }
    }

    public static RoadmapProgressCalculationResult CalculateRoadmapProgress(
        IReadOnlyList<RoadmapNode> nodes,
        IReadOnlyList<RoadmapEdge> edges,
        IReadOnlyDictionary<Guid, string> statusByNodeId)
    {
        var nodeById = nodes.ToDictionary(n => n.RoadmapNodeId);
        var rootNodes = nodes
            .Where(n => n.ParentNodeId == null && n.IsRequired)
            .OrderBy(n => n.LayoutRank ?? int.MaxValue)
            .ThenBy(n => n.LayoutOrder)
            .ThenBy(n => n.OrderIndex)
            .ToList();

        if (rootNodes.Count == 0)
        {
            rootNodes = nodes
                .Where(n => n.IsRequired && n.LayoutRole == "trunk")
                .OrderBy(n => n.LayoutRank ?? int.MaxValue)
                .ThenBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex)
                .ToList();
        }

        var visited = new HashSet<Guid>();
        var total = 0;
        var completed = 0;

        foreach (var node in rootNodes)
        {
            var units = CountUnits(node, visited);
            total += units.total;
            completed += units.completed;
        }

        if (total == 0)
        {
            foreach (var node in nodes.Where(IsManualProgressNode))
            {
                total++;
                if (statusByNodeId.GetValueOrDefault(node.RoadmapNodeId) == "completed")
                {
                    completed++;
                }
            }
        }

        return new RoadmapProgressCalculationResult
        {
            TotalUnits = total,
            CompletedUnits = completed,
            ProgressPercent = total == 0 ? 0 : Math.Round(completed * 100m / total, 2)
        };

        (int total, int completed) CountUnits(RoadmapNode node, HashSet<Guid> path)
        {
            if (!path.Add(node.RoadmapNodeId))
            {
                return (0, 0);
            }

            if (!node.IsRequired)
            {
                path.Remove(node.RoadmapNodeId);
                return (0, 0);
            }

            if (node.NodeType == "choice_group")
            {
                var children = GetRequiredChildren(node).ToList();
                var requiredCount = GetRequiredCount(node, children.Count);
                var completedCount = children.Count(child => IsSatisfied(statusByNodeId.GetValueOrDefault(child.RoadmapNodeId)));

                path.Remove(node.RoadmapNodeId);
                return (requiredCount, Math.Min(requiredCount, completedCount));
            }

            if (IsManualProgressNode(node))
            {
                path.Remove(node.RoadmapNodeId);
                return (1, IsSatisfied(statusByNodeId.GetValueOrDefault(node.RoadmapNodeId)) ? 1 : 0);
            }

            var total = 0;
            var completed = 0;

            foreach (var child in GetRequiredChildren(node))
            {
                var childUnits = CountUnits(child, path);
                total += childUnits.total;
                completed += childUnits.completed;
            }

            path.Remove(node.RoadmapNodeId);
            return (total, completed);
        }

        IEnumerable<RoadmapNode> GetRequiredChildren(RoadmapNode node)
        {
            var childIds = edges
                .Where(e =>
                    e.FromNodeId == node.RoadmapNodeId &&
                    e.DependencyType == "required" &&
                    e.EdgeType is "contains" or "choice")
                .Select(e => e.ToNodeId)
                .ToHashSet();

            var children = childIds.Count > 0
                ? nodes.Where(n => childIds.Contains(n.RoadmapNodeId))
                : nodes.Where(n => n.ParentNodeId == node.RoadmapNodeId);

            return children
                .Where(n => n.IsRequired)
                .OrderBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex);
        }
    }

    private static bool IsSatisfied(string? status) => status is "completed" or "skipped";

    private static int GetRequiredCount(RoadmapNode node, int childCount)
    {
        return node.SelectionType switch
        {
            "choose_one" => 1,
            "choose_many" => node.RequiredCount ?? 1,
            "complete_all" => childCount,
            _ => childCount
        };
    }
}
