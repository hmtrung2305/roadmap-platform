using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobPortalSource
{
    public Guid JobPortalSourceId { get; set; }

    public string Name { get; set; } = null!;

    public string BaseUrl { get; set; } = null!;

    public string SearchUrlTemplate { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public DateTime? LastScrapedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}
