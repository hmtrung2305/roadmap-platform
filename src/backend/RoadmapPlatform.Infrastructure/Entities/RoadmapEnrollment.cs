using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapEnrollment
{
    public Guid RoadmapEnrollmentId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public string Status { get; set; } = null!;

    public decimal ProgressPercent { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ProgressEvent> ProgressEvents { get; set; } = new List<ProgressEvent>();

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserNodeProgress> UserNodeProgresses { get; set; } = new List<UserNodeProgress>();
}
