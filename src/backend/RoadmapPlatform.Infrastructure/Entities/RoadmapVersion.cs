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

    public int MajorVersion { get; set; }

    public int MinorVersion { get; set; }

    public int PatchVersion { get; set; }

    public string ReleaseType { get; set; } = null!;

    public Guid? CreatedFromVersionId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual RoadmapVersion? CreatedFromVersion { get; set; }

    public virtual ICollection<RoadmapVersion> InverseCreatedFromVersion { get; set; } = new List<RoadmapVersion>();

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual ICollection<RoadmapEdge> RoadmapEdges { get; set; } = new List<RoadmapEdge>();

    public virtual ICollection<RoadmapEnrollment> RoadmapEnrollments { get; set; } = new List<RoadmapEnrollment>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();
}
