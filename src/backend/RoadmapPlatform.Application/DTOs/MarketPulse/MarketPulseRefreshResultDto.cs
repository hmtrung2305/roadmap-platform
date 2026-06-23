namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseRefreshResultDto
{
    public Guid? RunId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string Status { get; set; } = "success";

    public string Mode { get; set; } = "scheduled";

    public DateTime SnapshotDate { get; set; }

    public int SourcesScraped { get; set; }

    public int PostingsScraped { get; set; }

    public int PostingsSaved { get; set; }

    public int PostingsDuplicated { get; set; }

    public int PostingsFailed { get; set; }

    public int PostingsInserted { get; set; }

    public int PostingsUpdated { get; set; }

    public int PostingsSeen { get; set; }

    public int PostingsExpired { get; set; }

    public int NewPostings { get; set; }

    public int UpdatedPostings { get; set; }

    public int ActivePostings { get; set; }

    public int StalePostings { get; set; }

    public int ExpiredPostings { get; set; }
    
    public int SkillSnapshotsSaved { get; set; }

    public int TotalFetched
    {
        get => PostingsScraped;
        set => PostingsScraped = value;
    }

    public int TotalSaved
    {
        get => PostingsSaved;
        set => PostingsSaved = value;
    }

    public int TotalSkippedDuplicated
    {
        get => PostingsDuplicated;
        set => PostingsDuplicated = value;
    }

    public int TotalFailed
    {
        get => PostingsFailed;
        set => PostingsFailed = value;
    }

    public string? ErrorSummary { get; set; }
}
