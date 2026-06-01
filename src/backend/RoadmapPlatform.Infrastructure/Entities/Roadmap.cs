using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Roadmap
{
    public Guid RoadmapId { get; set; }

    public Guid SpecialtyId { get; set; }

    public string RoadmapName { get; set; } = null!;

    public int Version { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RoadmapEdge> RoadmapEdges { get; set; } = new List<RoadmapEdge>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual Specialty Specialty { get; set; } = null!;

    public virtual ICollection<UserRoadmapStatus> UserRoadmapStatuses { get; set; } = new List<UserRoadmapStatus>();
}
