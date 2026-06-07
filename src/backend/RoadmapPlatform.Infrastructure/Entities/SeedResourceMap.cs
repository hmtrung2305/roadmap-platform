using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SeedResourceMap
{
    public string? ResourceKey { get; set; }

    public Guid? LearningResourceId { get; set; }
}
