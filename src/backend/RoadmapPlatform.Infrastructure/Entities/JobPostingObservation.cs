using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobPostingObservation
{
    public Guid JobPostingObservationId { get; set; }

    public Guid JobPostingId { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public string SourceName { get; set; } = null!;

    public string ObservationStatus { get; set; } = null!;

    public string ContentHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime ObservedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual JobPosting JobPosting { get; set; } = null!;
}
