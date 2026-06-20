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

    public decimal ReadinessPercent { get; set; }

    public decimal SkillCoveragePercent { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
