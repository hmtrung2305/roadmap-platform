namespace RoadmapPlatform.Infrastructure.Entities;

public sealed class MarketPulsePublicationHistoryState
{
    public short SingletonId { get; set; } = 1;

    public DateTime CoverageStart { get; set; }

    public DateTime CoverageEnd { get; set; }

    public DateTime SourceDataAt { get; set; }

    public DateTime LastSuccessfulSyncAt { get; set; }

    public int SyncedPostingCount { get; set; }

    public DateTime UpdatedAt { get; set; }
}
