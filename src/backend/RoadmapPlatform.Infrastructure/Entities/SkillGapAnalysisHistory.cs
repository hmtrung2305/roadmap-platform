using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillGapAnalysisHistory
{
    public Guid SkillGapAnalysisHistoryId { get; set; }

    public Guid UserId { get; set; }

    public Guid CareerRoleId { get; set; }

    public string CareerRoleSlug { get; set; } = null!;

    public string CareerRoleName { get; set; } = null!;

    public string LevelName { get; set; } = null!;

    public string LevelSlug { get; set; } = null!;

    public int MatchedSkills { get; set; }

    public int TotalSkills { get; set; }

    public int MissingSkills { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int RoadmapVersionNumber { get; set; }

    public string RoadmapVersionTitle { get; set; } = null!;

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
