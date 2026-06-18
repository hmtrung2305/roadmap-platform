using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserProfile
{
    public Guid UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? Location { get; set; }

    public string? AvatarUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? CareerGoal { get; set; }

    public string? CurrentRole { get; set; }

    public string? PublicEmail { get; set; }

    public string? GithubUrl { get; set; }

    public string? LinkedinUrl { get; set; }

    public string? ResumeUrl { get; set; }

    public string? PersonalWebsiteUrl { get; set; }

    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual User User { get; set; } = null!;
}
