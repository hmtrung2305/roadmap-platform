using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class CareerRole
{
    public Guid CareerRoleId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CareerRoleSkillGroup> CareerRoleSkillGroups { get; set; } = new List<CareerRoleSkillGroup>();

    public virtual ICollection<CareerRoleSkill> CareerRoleSkills { get; set; } = new List<CareerRoleSkill>();

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();
}
