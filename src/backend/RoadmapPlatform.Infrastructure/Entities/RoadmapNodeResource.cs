using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapNodeResource
{
    public Guid RoadmapNodeResourceId { get; set; }

    public Guid RoadmapNodeId { get; set; }

    public Guid LearningResourceId { get; set; }

    public virtual LearningResource LearningResource { get; set; } = null!;

    public virtual RoadmapNode RoadmapNode { get; set; } = null!;
}
