using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillGapAnalysisHistory
{
    public Guid SkillGapAnalysisHistoryId { get; set; }

    public Guid UserId { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid RoadmapId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public string CareerRoleNameSnapshot { get; set; } = null!;

    public string RoadmapTitleSnapshot { get; set; } = null!;

    public string RoadmapVersionTitleSnapshot { get; set; } = null!;

    public string AuthorNameSnapshot { get; set; } = null!;

    public int MatchedSkills { get; set; }

    public int TotalSkills { get; set; }

    public int MissingSkills { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
