using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Specialty
{
    public Guid SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();
}
