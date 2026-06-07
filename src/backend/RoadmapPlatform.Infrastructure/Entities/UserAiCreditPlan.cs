using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserAiCreditPlan
{
    public Guid UserId { get; set; }

    public string PlanCode { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AiCreditPlan PlanCodeNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
