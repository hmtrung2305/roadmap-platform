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

    public virtual ICollection<AiMentorConversation> AiMentorConversations { get; set; } = new List<AiMentorConversation>();

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<ProgressEvent> ProgressEvents { get; set; } = new List<ProgressEvent>();

    public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();

    public virtual ICollection<RoadmapEnrollment> RoadmapEnrollments { get; set; } = new List<RoadmapEnrollment>();

    public virtual ICollection<RoadmapVersion> RoadmapVersions { get; set; } = new List<RoadmapVersion>();

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<SkillGapAnalysisHistory> SkillGapAnalysisHistories { get; set; } = new List<SkillGapAnalysisHistory>();

    public virtual ICollection<SkillModuleEnrollment> SkillModuleEnrollments { get; set; } = new List<SkillModuleEnrollment>();

    public virtual ICollection<SkillModuleQuizAttempt> SkillModuleQuizAttempts { get; set; } = new List<SkillModuleQuizAttempt>();

    public virtual ICollection<SkillModule> SkillModules { get; set; } = new List<SkillModule>();

    public virtual UserActivityStat? UserActivityStat { get; set; }

    public virtual UserAiCreditPlan? UserAiCreditPlan { get; set; }

    public virtual ICollection<UserAuthProvider> UserAuthProviders { get; set; } = new List<UserAuthProvider>();

    public virtual ICollection<UserInsight> UserInsights { get; set; } = new List<UserInsight>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
