using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class CareerRoleSkillGroup
{
    public Guid CareerRoleSkillGroupId { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid SkillGroupId { get; set; }

    public int Priority { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual SkillGroup SkillGroup { get; set; } = null!;
}
