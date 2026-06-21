using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserSkillGapAnalysis
{
    public Guid UserSkillGapAnalysisId { get; set; }

    public Guid UserId { get; set; }

    public Guid CareerRoleId { get; set; }

    public decimal ReadinessPercent { get; set; }

    public int CompletedGroups { get; set; }

    public int TotalGroups { get; set; }

    public string AnalysisResultJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int TotalSkills { get; set; }

    public int MatchedSkills { get; set; }

    public decimal SkillCoveragePercent { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
