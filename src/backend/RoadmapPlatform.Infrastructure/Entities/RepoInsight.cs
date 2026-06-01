using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RepoInsight
{
    public Guid InsightId { get; set; }

    public Guid RepositoryId { get; set; }

    public string? Summary { get; set; }

    public string? TechStack { get; set; }

    public string? DetectedSkills { get; set; }

    public string? ProjectType { get; set; }

    public DateTime AnalyzedAt { get; set; }

    public virtual Repository Repository { get; set; } = null!;
}
