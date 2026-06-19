using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class JobSkillMention
{
    public Guid JobSkillMentionId { get; set; }

    public Guid JobPostingId { get; set; }

    public Guid SkillTaxonomyId { get; set; }

    public string SourceName { get; set; } = null!;

    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public string MentionSource { get; set; } = null!;

    public DateOnly SnapshotDate { get; set; }

    public DateTime ObservedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual JobPosting JobPosting { get; set; } = null!;

    public virtual SkillTaxonomy SkillTaxonomy { get; set; } = null!;
}
