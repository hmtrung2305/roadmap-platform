using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Skill
{
    public Guid SkillId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CareerRoleSkill> CareerRoleSkills { get; set; } = new List<CareerRoleSkill>();

    public virtual ICollection<LearningResourceSkill> LearningResourceSkills { get; set; } = new List<LearningResourceSkill>();

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

    public virtual ICollection<RoadmapNodeSkill> RoadmapNodeSkills { get; set; } = new List<RoadmapNodeSkill>();

    public virtual ICollection<SkillGroupItem> SkillGroupItems { get; set; } = new List<SkillGroupItem>();
}
