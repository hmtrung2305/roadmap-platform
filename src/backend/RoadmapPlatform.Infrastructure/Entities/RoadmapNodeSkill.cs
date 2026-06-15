using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapNodeSkill
{
    public Guid RoadmapNodeSkillId { get; set; }

    public Guid RoadmapNodeId { get; set; }

    public Guid SkillId { get; set; }

    public virtual RoadmapNode RoadmapNode { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
