using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapNode
{
    public Guid RoadmapNodeId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public Guid? ParentNodeId { get; set; }

    public string Slug { get; set; } = null!;

    public string NodeType { get; set; } = null!;

    public string? CheckpointType { get; set; }

    public string? SelectionType { get; set; }

    public int? RequiredCount { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Reason { get; set; }

    public int OrderIndex { get; set; }

    public string LayoutRole { get; set; } = null!;

    public string? LayoutGroup { get; set; }

    public int? LayoutRank { get; set; }

    public int LayoutOrder { get; set; }

    public int? EstimatedHours { get; set; }

    public string? DifficultyLevel { get; set; }

    public int Priority { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public string Metadata { get; set; } = null!;

    public bool IsRequired { get; set; }

    public bool IsTrackable { get; set; }

    public string LearningOutcomes { get; set; } = null!;

    public string CompletionCriteria { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RoadmapNode> InverseParentNode { get; set; } = new List<RoadmapNode>();

    public virtual RoadmapNode? ParentNode { get; set; }

    public virtual ICollection<RoadmapEdge> RoadmapEdgeRoadmapNodeNavigations { get; set; } = new List<RoadmapEdge>();

    public virtual ICollection<RoadmapEdge> RoadmapEdgeRoadmapNodes { get; set; } = new List<RoadmapEdge>();

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;
}
