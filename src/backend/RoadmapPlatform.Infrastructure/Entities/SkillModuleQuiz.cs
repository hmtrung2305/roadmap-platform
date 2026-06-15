using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleQuiz
{
    public Guid SkillModuleQuizId { get; set; }

    public Guid SkillModuleId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PassingScorePercent { get; set; }

    public int? MaxAttempts { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SkillModule SkillModule { get; set; } = null!;

    public virtual ICollection<SkillModuleQuizAttempt> SkillModuleQuizAttempts { get; set; } = new List<SkillModuleQuizAttempt>();

    public virtual ICollection<SkillModuleQuizQuestion> SkillModuleQuizQuestions { get; set; } = new List<SkillModuleQuizQuestion>();
}
