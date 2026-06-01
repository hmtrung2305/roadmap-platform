using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Skill
{
    public Guid SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<NodeSkill> NodeSkills { get; set; } = new List<NodeSkill>();

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

    public virtual ICollection<UserSkillProgress> UserSkillProgresses { get; set; } = new List<UserSkillProgress>();
}
