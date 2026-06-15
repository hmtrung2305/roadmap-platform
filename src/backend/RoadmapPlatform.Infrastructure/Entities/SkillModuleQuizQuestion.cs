using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleQuizQuestion
{
    public Guid SkillModuleQuizQuestionId { get; set; }

    public Guid SkillModuleQuizId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public int Points { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SkillModuleQuiz SkillModuleQuiz { get; set; } = null!;

    public virtual ICollection<SkillModuleQuizAnswer> SkillModuleQuizAnswers { get; set; } = new List<SkillModuleQuizAnswer>();

    public virtual ICollection<SkillModuleQuizOption> SkillModuleQuizOptions { get; set; } = new List<SkillModuleQuizOption>();
}
