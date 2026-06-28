using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserNodeProgress
{
    public Guid UserNodeProgressId { get; set; }

    public Guid RoadmapEnrollmentId { get; set; }

    public Guid RoadmapNodeId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? SkippedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual RoadmapEnrollment RoadmapEnrollment { get; set; } = null!;

    public virtual RoadmapNode RoadmapNode { get; set; } = null!;
}
