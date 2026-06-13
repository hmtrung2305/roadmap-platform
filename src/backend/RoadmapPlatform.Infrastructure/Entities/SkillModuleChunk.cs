using System;
using System.Collections.Generic;
using Pgvector;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleChunk
{
    public Guid SkillModuleChunkId { get; set; }

    public Guid SkillModuleId { get; set; }

    public Guid SkillModuleLessonId { get; set; }

    public int ChunkIndex { get; set; }

    public string? Heading { get; set; }

    public string Content { get; set; } = null!;

    public Vector? Embedding { get; set; }

    public int? TokenCount { get; set; }

    public string? ContentHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SkillModule SkillModule { get; set; } = null!;

    public virtual SkillModuleLesson SkillModuleLesson { get; set; } = null!;
}
