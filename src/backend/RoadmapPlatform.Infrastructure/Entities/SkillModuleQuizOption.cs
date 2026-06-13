using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleQuizOption
{
    public Guid SkillModuleQuizOptionId { get; set; }

    public Guid SkillModuleQuizQuestionId { get; set; }

    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<SkillModuleQuizAnswer> SkillModuleQuizAnswers { get; set; } = new List<SkillModuleQuizAnswer>();

    public virtual SkillModuleQuizQuestion SkillModuleQuizQuestion { get; set; } = null!;
}
