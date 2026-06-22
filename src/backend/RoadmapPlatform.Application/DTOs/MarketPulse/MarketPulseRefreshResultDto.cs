namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseRefreshResultDto
{
    public DateTime SnapshotDate { get; set; }

    public int SourcesScraped { get; set; }

    public int PostingsScraped { get; set; }

    public int PostingsSaved { get; set; }

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
}
