using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class OtherResource
{
    public Guid ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string? Provider { get; set; }

    public virtual Resource Resource { get; set; } = null!;
}
