using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Resource
{
    public Guid ResourceId { get; set; }

    public Guid SkillId { get; set; }

    public string? Title { get; set; }

    public string Url { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? Metadata { get; set; }

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual MyResource? MyResource { get; set; }

    public virtual OtherResource? OtherResource { get; set; }

    public virtual ICollection<ResourceChunk> ResourceChunks { get; set; } = new List<ResourceChunk>();

    public virtual Skill Skill { get; set; } = null!;
}
