using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserInsight
{
    public Guid InsightId { get; set; }

    public Guid UserId { get; set; }

    public string? Metadata { get; set; }

    public virtual User User { get; set; } = null!;
}
