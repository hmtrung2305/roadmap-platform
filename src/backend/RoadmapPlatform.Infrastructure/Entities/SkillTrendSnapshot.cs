using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillTrendSnapshot
{
    public Guid SkillTrendSnapshotId { get; set; }

    public DateTime SnapshotDate { get; set; }

    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public string SourceName { get; set; } = null!;

    public int MentionCount { get; set; }

    public int PostingCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
