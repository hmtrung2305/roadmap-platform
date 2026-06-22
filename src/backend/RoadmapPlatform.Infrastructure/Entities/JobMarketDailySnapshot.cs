using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobMarketDailySnapshot
{
    public Guid JobMarketDailySnapshotId { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public string SourceName { get; set; } = null!;

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? SkillSlug { get; set; }

    public string? SkillName { get; set; }

    public int ActiveJobCount { get; set; }

    public int NewJobCount { get; set; }

    public int ObservedJobCount { get; set; }

    public int MentionCount { get; set; }

    public int SalarySampleCount { get; set; }

    public int? SalaryMin { get; set; }

    public int? SalaryMax { get; set; }

    public decimal? ExperienceMinYears { get; set; }

    public decimal? ExperienceMaxYears { get; set; }

    public int SampleSize { get; set; }

    public string Confidence { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
