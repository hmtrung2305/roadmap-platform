using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserActivityStat
{
    public Guid UserId { get; set; }

    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    public DateTime? LastInteraction { get; set; }

    public virtual User User { get; set; } = null!;
}
