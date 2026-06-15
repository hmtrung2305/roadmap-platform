using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleEnrollment
{
    public Guid SkillModuleEnrollmentId { get; set; }

    public Guid UserId { get; set; }

    public Guid SkillModuleId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? LastAccessedLessonId { get; set; }

    public decimal ProgressPercent { get; set; }

    public string LessonProgress { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SkillModuleLesson? LastAccessedLesson { get; set; }

    public virtual SkillModule SkillModule { get; set; } = null!;

    public virtual ICollection<SkillModuleQuizAttempt> SkillModuleQuizAttempts { get; set; } = new List<SkillModuleQuizAttempt>();

    public virtual User User { get; set; } = null!;
}
