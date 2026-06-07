using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

internal sealed class RoadmapEstimatedTimeResult
{
    public decimal EstimatedRequiredHours { get; init; }
    public decimal EstimatedOptionalHours { get; init; }
    public IReadOnlyDictionary<Guid, RoadmapNodeEstimatedTime> NodeEstimates { get; init; } =
        new Dictionary<Guid, RoadmapNodeEstimatedTime>();
}

internal sealed class RoadmapNodeEstimatedTime
{
    public decimal EstimatedRequiredHours { get; init; }
    public decimal EstimatedOptionalHours { get; init; }
}

internal static class RoadmapEstimatedTimeCalculator
{
    private static readonly HashSet<string> ManualNodeTypes =
    [
        "topic",
        "choice_option",
        "checkpoint",
        "project"
    ];

    private static readonly HashSet<string> ChildEdgeTypes =
    [
        "contains",
        "choice"
    ];

    public static RoadmapEstimatedTimeResult Calculate(
        IReadOnlyList<RoadmapNode> nodes,
        IReadOnlyList<RoadmapEdge> edges)
    {
        var nodeById = nodes.ToDictionary(n => n.RoadmapNodeId);
        var memo = new Dictionary<Guid, RoadmapNodeEstimatedTime>();
        var visiting = new HashSet<Guid>();

        foreach (var node in nodes)
        {
            CalculateNode(node.RoadmapNodeId);
        }

        var roots = nodes
            .Where(n => n.ParentNodeId == null)
            .OrderBy(n => n.LayoutRank ?? int.MaxValue)
            .ThenBy(n => n.LayoutOrder)
            .ThenBy(n => n.OrderIndex)
            .ToList();

        if (roots.Count == 0)
        {
            roots = nodes
                .Where(n => n.LayoutRole == "trunk")
                .OrderBy(n => n.LayoutRank ?? int.MaxValue)
                .ThenBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex)
                .ToList();
        }

        if (roots.Count == 0)
        {
            roots = nodes
                .Where(n => ManualNodeTypes.Contains(n.NodeType))
                .OrderBy(n => n.LayoutRank ?? int.MaxValue)
                .ThenBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex)
                .ToList();
        }

        var totalRequired = 0m;
        var totalOptional = 0m;
        var counted = new HashSet<Guid>();

        foreach (var root in roots)
        {
            CountRoot(root, counted, ref totalRequired, ref totalOptional);
        }

        return new RoadmapEstimatedTimeResult
        {
            EstimatedRequiredHours = RoundHours(totalRequired),
            EstimatedOptionalHours = RoundHours(totalOptional),
            NodeEstimates = memo.ToDictionary(
                pair => pair.Key,
                pair => new RoadmapNodeEstimatedTime
                {
                    EstimatedRequiredHours = RoundHours(pair.Value.EstimatedRequiredHours),
                    EstimatedOptionalHours = RoundHours(pair.Value.EstimatedOptionalHours)
                })
        };

        void CountRoot(
            RoadmapNode node,
            HashSet<Guid> countedNodes,
            ref decimal requiredHours,
            ref decimal optionalHours)
        {
            if (!countedNodes.Add(node.RoadmapNodeId))
            {
                return;
            }

            var estimate = CalculateNode(node.RoadmapNodeId);
            requiredHours += estimate.EstimatedRequiredHours;
            optionalHours += estimate.EstimatedOptionalHours;
        }

        RoadmapNodeEstimatedTime CalculateNode(Guid nodeId)
        {
            if (memo.TryGetValue(nodeId, out var cached))
            {
                return cached;
            }

            if (!nodeById.TryGetValue(nodeId, out var node))
            {
                return Zero();
            }

            if (!visiting.Add(nodeId))
            {
                return Zero();
            }

            RoadmapNodeEstimatedTime result;

            if (ManualNodeTypes.Contains(node.NodeType))
            {
                var hours = ToHours(node.EstimatedHours);
                result = node.IsRequired
                    ? new RoadmapNodeEstimatedTime { EstimatedRequiredHours = hours, EstimatedOptionalHours = 0 }
                    : new RoadmapNodeEstimatedTime { EstimatedRequiredHours = 0, EstimatedOptionalHours = hours };
            }
            else if (node.NodeType == "choice_group")
            {
                result = CalculateChoiceGroup(node);
            }
            else
            {
                result = CalculateContainer(node);
            }

            visiting.Remove(nodeId);
            memo[nodeId] = result;
            return result;
        }

        RoadmapNodeEstimatedTime CalculateContainer(RoadmapNode node)
        {
            var children = GetChildren(node).ToList();

            if (children.Count == 0)
            {
                var ownHours = ToHours(node.EstimatedHours);
                return node.IsRequired
                    ? new RoadmapNodeEstimatedTime { EstimatedRequiredHours = ownHours, EstimatedOptionalHours = 0 }
                    : new RoadmapNodeEstimatedTime { EstimatedRequiredHours = 0, EstimatedOptionalHours = ownHours };
            }

            var required = 0m;
            var optional = 0m;

            foreach (var child in children)
            {
                var estimate = CalculateNode(child.RoadmapNodeId);
                required += estimate.EstimatedRequiredHours;
                optional += estimate.EstimatedOptionalHours;
            }

            if (node.IsRequired)
            {
                return new RoadmapNodeEstimatedTime
                {
                    EstimatedRequiredHours = required,
                    EstimatedOptionalHours = optional
                };
            }

            return new RoadmapNodeEstimatedTime
            {
                EstimatedRequiredHours = 0,
                EstimatedOptionalHours = required + optional
            };
        }

        RoadmapNodeEstimatedTime CalculateChoiceGroup(RoadmapNode node)
        {
            var children = GetChildren(node).ToList();

            if (children.Count == 0)
            {
                return Zero();
            }

            var requiredChildren = children.Where(c => c.IsRequired).ToList();
            var optionalChildren = children.Where(c => !c.IsRequired).ToList();

            var requiredChildEstimates = requiredChildren
                .Select(child => CalculateNode(child.RoadmapNodeId))
                .ToList();

            var requiredChildRequiredTotal = requiredChildEstimates.Sum(e => e.EstimatedRequiredHours);
            var requiredChildOptionalTotal = requiredChildEstimates.Sum(e => e.EstimatedOptionalHours);
            var optionalChildTotal = optionalChildren
                .Select(child => CalculateNode(child.RoadmapNodeId))
                .Sum(e => e.EstimatedRequiredHours + e.EstimatedOptionalHours);

            var selectedRequiredHours = node.SelectionType switch
            {
                "choose_one" => Average(requiredChildEstimates.Select(e => e.EstimatedRequiredHours)),
                "choose_many" => Average(requiredChildEstimates.Select(e => e.EstimatedRequiredHours))
                    * Math.Min(GetRequiredCount(node, requiredChildren.Count), requiredChildren.Count),
                "complete_all" => requiredChildRequiredTotal,
                _ => requiredChildRequiredTotal
            };

            var optionalHours = Math.Max(0, requiredChildRequiredTotal - selectedRequiredHours)
                + requiredChildOptionalTotal
                + optionalChildTotal;

            if (node.IsRequired)
            {
                return new RoadmapNodeEstimatedTime
                {
                    EstimatedRequiredHours = selectedRequiredHours,
                    EstimatedOptionalHours = optionalHours
                };
            }

            return new RoadmapNodeEstimatedTime
            {
                EstimatedRequiredHours = 0,
                EstimatedOptionalHours = selectedRequiredHours + optionalHours
            };
        }

        IEnumerable<RoadmapNode> GetChildren(RoadmapNode node)
        {
            var childIds = edges
                .Where(e => e.FromNodeId == node.RoadmapNodeId && ChildEdgeTypes.Contains(e.EdgeType))
                .Select(e => e.ToNodeId)
                .ToHashSet();

            var children = childIds.Count > 0
                ? nodes.Where(n => childIds.Contains(n.RoadmapNodeId))
                : nodes.Where(n => n.ParentNodeId == node.RoadmapNodeId);

            return children
                .OrderBy(n => n.LayoutOrder)
                .ThenBy(n => n.OrderIndex)
                .ThenBy(n => n.Title);
        }
    }

    private static int GetRequiredCount(RoadmapNode node, int childCount)
    {
        return node.SelectionType switch
        {
            "choose_one" => 1,
            "choose_many" => Math.Max(1, node.RequiredCount ?? 1),
            "complete_all" => childCount,
            _ => childCount
        };
    }

    private static decimal Average(IEnumerable<decimal> values)
    {
        var list = values.Where(value => value > 0).ToList();
        return list.Count == 0 ? 0 : list.Average();
    }

    private static decimal ToHours(int? hours)
    {
        return hours.HasValue && hours.Value > 0 ? hours.Value : 0m;
    }

    private static decimal RoundHours(decimal hours)
    {
        return Math.Round(hours, 2, MidpointRounding.AwayFromZero);
    }

    private static RoadmapNodeEstimatedTime Zero()
    {
        return new RoadmapNodeEstimatedTime
        {
            EstimatedRequiredHours = 0,
            EstimatedOptionalHours = 0
        };
    }
}
