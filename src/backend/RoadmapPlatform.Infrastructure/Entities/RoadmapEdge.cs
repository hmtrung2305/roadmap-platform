using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapEdge
{
    public Guid RoadmapEdgeId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public Guid FromNodeId { get; set; }

    public Guid ToNodeId { get; set; }

    public string EdgeType { get; set; } = null!;

    public string DependencyType { get; set; } = null!;

    public string Condition { get; set; } = null!;

    public virtual RoadmapNode RoadmapNode { get; set; } = null!;

    public virtual RoadmapNode RoadmapNodeNavigation { get; set; } = null!;

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;
}
