using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapNode
{
    public Guid NodeId { get; set; }

    public Guid RoadmapId { get; set; }

    public string Title { get; set; } = null!;

    public double? PositionX { get; set; }

    public double? PositionY { get; set; }

    public string? Description { get; set; }

    public bool IsMandatory { get; set; }

    public virtual ICollection<NodeSkill> NodeSkills { get; set; } = new List<NodeSkill>();

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual ICollection<RoadmapEdge> RoadmapEdgeAncestorNodes { get; set; } = new List<RoadmapEdge>();

    public virtual ICollection<RoadmapEdge> RoadmapEdgeDescendantNodes { get; set; } = new List<RoadmapEdge>();
}
