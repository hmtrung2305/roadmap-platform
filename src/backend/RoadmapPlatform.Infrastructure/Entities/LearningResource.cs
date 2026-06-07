using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class LearningResource
{
    public Guid LearningResourceId { get; set; }

    public string Title { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string ResourceType { get; set; } = null!;

    public string? Description { get; set; }

    public string? Provider { get; set; }

    public string? DifficultyLevel { get; set; }

    public string LanguageCode { get; set; } = null!;

    public string VerificationStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<LearningResourceSkill> LearningResourceSkills { get; set; } = new List<LearningResourceSkill>();

    public virtual ICollection<RoadmapNodeResource> RoadmapNodeResources { get; set; } = new List<RoadmapNodeResource>();
}
