using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobPostingVersion
{
    public Guid JobPostingVersionId { get; set; }

    public Guid JobPostingId { get; set; }

    public string ContentHash { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Salary { get; set; }

    public string? Experience { get; set; }

    public string Description { get; set; } = null!;

    public string Requirements { get; set; } = null!;

    public string Specialties { get; set; } = null!;

    public string Benefits { get; set; } = null!;

    public string Skills { get; set; } = null!;

    public DateTime ObservedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual JobPosting JobPosting { get; set; } = null!;
}
