using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModuleLesson
{
    public Guid SkillModuleLessonId { get; set; }

    public Guid SkillModuleId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Summary { get; set; }

    public int OrderIndex { get; set; }

    public decimal? EstimatedHours { get; set; }

    public string MarkdownFileKey { get; set; } = null!;

    public string? MarkdownFileName { get; set; }

    public string? ContentHash { get; set; }

    public long? ContentSizeBytes { get; set; }

    public int ContentVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string IndexingStatus { get; set; } = null!;

    public DateTime? IndexedAt { get; set; }

    public string? IndexingError { get; set; }

    public virtual SkillModule SkillModule { get; set; } = null!;

    public virtual ICollection<SkillModuleChunk> SkillModuleChunks { get; set; } = new List<SkillModuleChunk>();

    public virtual ICollection<SkillModuleEnrollment> SkillModuleEnrollments { get; set; } = new List<SkillModuleEnrollment>();
}
