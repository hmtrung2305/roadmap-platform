using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MyResource
{
    public Guid ResourceId { get; set; }

    public virtual Resource Resource { get; set; } = null!;
}
