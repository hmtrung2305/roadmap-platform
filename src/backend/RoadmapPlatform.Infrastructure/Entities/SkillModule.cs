using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class SkillModule
{
    public Guid SkillModuleId { get; set; }

    public Guid SkillId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }

    public string Status { get; set; } = null!;

    public Guid? CreatedByUserId { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public string Metadata { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual ICollection<SkillModuleChunk> SkillModuleChunks { get; set; } = new List<SkillModuleChunk>();

    public virtual ICollection<SkillModuleEnrollment> SkillModuleEnrollments { get; set; } = new List<SkillModuleEnrollment>();

    public virtual ICollection<SkillModuleLesson> SkillModuleLessons { get; set; } = new List<SkillModuleLesson>();

    public virtual SkillModuleQuiz? SkillModuleQuiz { get; set; }
}
