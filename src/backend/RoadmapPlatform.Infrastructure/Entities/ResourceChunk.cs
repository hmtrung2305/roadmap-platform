using System;
using System.Collections.Generic;
using Pgvector;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class ResourceChunk
{
    public Guid ChunkId { get; set; }

    public Guid ResourceId { get; set; }

    public string ChunkContent { get; set; } = null!;

    public Vector? Embedding { get; set; }

    public virtual Resource Resource { get; set; } = null!;
}
