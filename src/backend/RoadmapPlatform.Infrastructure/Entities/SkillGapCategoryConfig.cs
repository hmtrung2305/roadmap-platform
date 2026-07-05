using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillGapCategoryConfig
{
    public Guid SkillGapCategoryConfigId { get; set; }

    public Guid RoadmapId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;
}
