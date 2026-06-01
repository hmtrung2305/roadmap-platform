using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserRoadmapStatus
{
    public Guid EnrollmentId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoadmapId { get; set; }

    public DateTime LastTime { get; set; }

    public string? Status { get; set; }

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
