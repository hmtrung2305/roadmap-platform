using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapEdge
{
    public Guid EdgeId { get; set; }

    public Guid RoadmapId { get; set; }

    public Guid AncestorNodeId { get; set; }

    public Guid DescendantNodeId { get; set; }

    public virtual RoadmapNode AncestorNode { get; set; } = null!;

    public virtual RoadmapNode DescendantNode { get; set; } = null!;

    public virtual Roadmap Roadmap { get; set; } = null!;
}
