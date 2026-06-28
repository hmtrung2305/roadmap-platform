using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapVersion
{
    public Guid RoadmapVersionId { get; set; }

    public Guid RoadmapId { get; set; }

    public int VersionNumber { get; set; }

    public string Status { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? EstimatedTotalHours { get; set; }

    public string LayoutDirection { get; set; } = null!;

    public string? LayoutAlgorithm { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual ICollection<RoadmapEdge> RoadmapEdges { get; set; } = new List<RoadmapEdge>();

    public virtual ICollection<RoadmapEnrollment> RoadmapEnrollments { get; set; } = new List<RoadmapEnrollment>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();
}
