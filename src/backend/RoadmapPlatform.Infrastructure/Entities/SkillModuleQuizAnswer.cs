using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleQuizAnswer
{
    public Guid SkillModuleQuizAnswerId { get; set; }

    public Guid SkillModuleQuizAttemptId { get; set; }

    public Guid SkillModuleQuizQuestionId { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public bool IsCorrect { get; set; }

    public int EarnedPoints { get; set; }

    public DateTime AnsweredAt { get; set; }

    public virtual SkillModuleQuizOption? SelectedOption { get; set; }

    public virtual SkillModuleQuizAttempt SkillModuleQuizAttempt { get; set; } = null!;

    public virtual SkillModuleQuizQuestion SkillModuleQuizQuestion { get; set; } = null!;
}
