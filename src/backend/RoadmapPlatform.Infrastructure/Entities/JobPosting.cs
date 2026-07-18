using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobPosting
{
    public Guid JobPostingId { get; set; }

    public Guid JobPortalSourceId { get; set; }

    public string ExternalId { get; set; } = null!;

    public string? SourceJobId { get; set; }

    public string Title { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Salary { get; set; }

    public string? SalaryRaw { get; set; }

    public long? SalaryMin { get; set; }

    public long? SalaryMax { get; set; }

    public string? SalaryCurrency { get; set; }

    public bool? SalaryIsNegotiable { get; set; }

    public string? Experience { get; set; }

    public string? ExperienceRaw { get; set; }

    public int? ExperienceMinYears { get; set; }

    public int? ExperienceMaxYears { get; set; }

    public string Url { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public string? PostDateText { get; set; }

    public string PostDateConfidence { get; set; } = "unknown";

    public DateTime? PostDateLowerBound { get; set; }

    public DateTime? PostDateUpperBound { get; set; }

    public DateTime? PostDateObservedOn { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }

    public string? DetailStatus { get; set; }

    public DateTime? DetailLastSuccessAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string Requirements { get; set; } = null!;

    public string Specialties { get; set; } = null!;

    public string Benefits { get; set; } = null!;

    public string Skills { get; set; } = null!;

    public string ContentHash { get; set; } = null!;

    public string LifecycleStatus { get; set; } = null!;

    public bool IsActive { get; set; }

    public int MissingScanCount { get; set; }

    public int SeenCount { get; set; }

    public int UpdatedScanCount { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime LastCheckedAt { get; set; }

    public DateTime? LastChangedAt { get; set; }

    public DateTime? ClosedDetectedAt { get; set; }

    public DateTime ScrapedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual JobPortalSource JobPortalSource { get; set; } = null!;

}
