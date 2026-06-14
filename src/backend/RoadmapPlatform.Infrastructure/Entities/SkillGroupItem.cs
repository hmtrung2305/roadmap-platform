using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillGroupItem
{
    public Guid SkillGroupItemId { get; set; }

    public Guid SkillGroupId { get; set; }

    public Guid SkillId { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual SkillGroup SkillGroup { get; set; } = null!;
}
