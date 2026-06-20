using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillTaxonomy
{
    public Guid SkillTaxonomyId { get; set; }

    public string SkillName { get; set; } = null!;

    public string SkillSlug { get; set; } = null!;

    public string? Category { get; set; }

    public string Aliases { get; set; } = null!;

    public string? PlatformSkillSlug { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<JobSkillMention> JobSkillMentions { get; set; } = new List<JobSkillMention>();
}
