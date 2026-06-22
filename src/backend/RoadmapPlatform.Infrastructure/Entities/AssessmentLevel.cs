using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AssessmentLevel
{
    public Guid AssessmentLevelId { get; set; }

    public Guid CareerRoleId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public int SortOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AssessmentLevelGroup> AssessmentLevelGroups { get; set; } = new List<AssessmentLevelGroup>();

    public virtual CareerRole CareerRole { get; set; } = null!;
}
