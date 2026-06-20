using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MarketPulseInsightSnapshot
{
    public Guid MarketPulseInsightSnapshotId { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public string SourceName { get; set; } = null!;

    public string InsightKey { get; set; } = null!;

    public string InsightType { get; set; } = null!;

    public int PeriodDays { get; set; }

    public int SampleSize { get; set; }

    public string Confidence { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
