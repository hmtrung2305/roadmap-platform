using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleQuizAttempt
{
    public Guid SkillModuleQuizAttemptId { get; set; }

    public Guid SkillModuleQuizId { get; set; }

    public Guid SkillModuleEnrollmentId { get; set; }

    public Guid UserId { get; set; }

    public int AttemptNo { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public decimal? ScorePercent { get; set; }

    public int? EarnedPoints { get; set; }

    public int? TotalPoints { get; set; }

    public bool? Passed { get; set; }

    public virtual SkillModuleEnrollment SkillModuleEnrollment { get; set; } = null!;

    public virtual SkillModuleQuiz SkillModuleQuiz { get; set; } = null!;

    public virtual ICollection<SkillModuleQuizAnswer> SkillModuleQuizAnswers { get; set; } = new List<SkillModuleQuizAnswer>();

    public virtual User User { get; set; } = null!;
}
