using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Roadmap
{
    public Guid RoadmapId { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string Visibility { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual User? OwnerUser { get; set; }

    public virtual ICollection<RoadmapVersion> RoadmapVersions { get; set; } = new List<RoadmapVersion>();

    public virtual ICollection<SkillGapAnalysisHistory> SkillGapAnalysisHistories { get; set; } = new List<SkillGapAnalysisHistory>();

    public virtual ICollection<SkillGapCategoryConfig> SkillGapCategoryConfigs { get; set; } = new List<SkillGapCategoryConfig>();
}
