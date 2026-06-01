using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserSkillProgress
{
    public Guid ProgressId { get; set; }

    public Guid UserId { get; set; }

    public Guid SkillId { get; set; }

    public string? Status { get; set; }

    public string? UnlockMethod { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
