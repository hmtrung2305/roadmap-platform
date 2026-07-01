using System;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class RoadmapVersionReviewEvent
{
    public Guid RoadmapVersionReviewEventId { get; set; }

    public Guid RoadmapVersionId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string EventType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }

    public virtual RoadmapVersion RoadmapVersion { get; set; } = null!;
}
