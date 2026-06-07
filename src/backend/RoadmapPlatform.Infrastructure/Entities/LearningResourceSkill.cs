using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class LearningResourceSkill
{
    public Guid LearningResourceSkillId { get; set; }

    public Guid LearningResourceId { get; set; }

    public Guid SkillId { get; set; }

    public virtual LearningResource LearningResource { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
