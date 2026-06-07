using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AiCreditUsage
{
    public Guid UsageId { get; set; }

    public Guid UserId { get; set; }

    public string FeatureName { get; set; } = null!;

    public int CreditCost { get; set; }

    public Guid? RequestRefId { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
