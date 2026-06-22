using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AssessmentLevelGroup
{
    public Guid AssessmentLevelGroupId { get; set; }

    public Guid AssessmentLevelId { get; set; }

    public Guid RoadmapNodeId { get; set; }

    public virtual AssessmentLevel AssessmentLevel { get; set; } = null!;

    public virtual RoadmapNode RoadmapNode { get; set; } = null!;
}
