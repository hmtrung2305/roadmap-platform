using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class ProgressEvent
{
    public Guid ProgressEventId { get; set; }

    public Guid RoadmapEnrollmentId { get; set; }

    public Guid RoadmapNodeId { get; set; }

    public Guid UserId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RoadmapEnrollment RoadmapEnrollment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
