using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class CareerRoleSkill
{
    public Guid CareerRoleSkillId { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid SkillId { get; set; }

    public int Priority { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
