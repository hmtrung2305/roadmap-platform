using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillGroup
{
    public Guid SkillGroupId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<CareerRoleSkillGroup> CareerRoleSkillGroups { get; set; } = new List<CareerRoleSkillGroup>();

    public virtual ICollection<SkillGroupItem> SkillGroupItems { get; set; } = new List<SkillGroupItem>();
}
