using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;

    public string UsernameNormalized { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AiCreditUsage> AiCreditUsages { get; set; } = new List<AiCreditUsage>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();

    public virtual UserActivityStat? UserActivityStat { get; set; }

    public virtual UserAiCreditPlan? UserAiCreditPlan { get; set; }

    public virtual ICollection<UserAuthProvider> UserAuthProviders { get; set; } = new List<UserAuthProvider>();

    public virtual ICollection<UserInsight> UserInsights { get; set; } = new List<UserInsight>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserRoadmapStatus> UserRoadmapStatuses { get; set; } = new List<UserRoadmapStatus>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserSkillProgress> UserSkillProgresses { get; set; } = new List<UserSkillProgress>();
}
