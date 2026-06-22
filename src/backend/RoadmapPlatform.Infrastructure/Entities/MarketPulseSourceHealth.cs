using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MarketPulseSourceHealth
{
    public Guid MarketPulseSourceHealthId { get; set; }

    public string SourceName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? LastSuccessAt { get; set; }

    public DateTime? LastFailureAt { get; set; }

    public int ConsecutiveFailures { get; set; }

    public Guid? LastRunId { get; set; }

    public string? LastErrorSummary { get; set; }

    public DateTime UpdatedAt { get; set; }
}
