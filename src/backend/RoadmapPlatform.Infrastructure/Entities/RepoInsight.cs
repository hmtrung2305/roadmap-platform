using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RepoInsight
{
    public Guid InsightId { get; set; }

    public Guid RepositoryId { get; set; }

    public string? Summary { get; set; }

    public string TechStack { get; set; } = null!;

    public string DetectedSkills { get; set; } = null!;

    public string? ProjectType { get; set; }

    public string AnalysisStatus { get; set; } = null!;

    public string? ReadmeHash { get; set; }

    public bool ReadmeTruncated { get; set; }

    public string? AiModel { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime AnalyzedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Repository Repository { get; set; } = null!;
}
