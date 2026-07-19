using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiCreditPlan> AiCreditPlans { get; set; }

    public virtual DbSet<AiCreditUsage> AiCreditUsages { get; set; }

    public virtual DbSet<AiMentorConversation> AiMentorConversations { get; set; }

    public virtual DbSet<AiMentorMessage> AiMentorMessages { get; set; }

    public virtual DbSet<CareerRole> CareerRoles { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<JobPosting> JobPostings { get; set; }

    public virtual DbSet<LearningResource> LearningResources { get; set; }

    public virtual DbSet<LearningResourceSkill> LearningResourceSkills { get; set; }

    public virtual DbSet<MarketPulseClassifierKeywordMapping> MarketPulseClassifierKeywordMappings { get; set; }

    public virtual DbSet<MarketPulseCrawlRun> MarketPulseCrawlRuns { get; set; }

    public virtual DbSet<MarketPulseFailedItem> MarketPulseFailedItems { get; set; }

    public virtual DbSet<MarketPulsePublicationHistoryState> MarketPulsePublicationHistoryStates { get; set; }

    public virtual DbSet<MarketPulseRefreshOperation> MarketPulseRefreshOperations { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<PendingLocalRegistration> PendingLocalRegistrations { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionRole> PermissionRoles { get; set; }

    public virtual DbSet<ProgressEvent> ProgressEvents { get; set; }

    public virtual DbSet<RepoInsight> RepoInsights { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<Roadmap> Roadmaps { get; set; }

    public virtual DbSet<RoadmapEdge> RoadmapEdges { get; set; }

    public virtual DbSet<RoadmapEnrollment> RoadmapEnrollments { get; set; }

    public virtual DbSet<RoadmapNode> RoadmapNodes { get; set; }

    public virtual DbSet<RoadmapNodeResource> RoadmapNodeResources { get; set; }

    public virtual DbSet<RoadmapNodeSkill> RoadmapNodeSkills { get; set; }

    public virtual DbSet<RoadmapVersion> RoadmapVersions { get; set; }

    public virtual DbSet<RoadmapVersionReviewEvent> RoadmapVersionReviewEvents { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SkillGapAnalysisHistory> SkillGapAnalysisHistories { get; set; }

    public virtual DbSet<SkillGapCategoryConfig> SkillGapCategoryConfigs { get; set; }

    public virtual DbSet<SkillModule> SkillModules { get; set; }

    public virtual DbSet<SkillModuleChunk> SkillModuleChunks { get; set; }

    public virtual DbSet<SkillModuleEnrollment> SkillModuleEnrollments { get; set; }

    public virtual DbSet<SkillModuleLesson> SkillModuleLessons { get; set; }

    public virtual DbSet<SkillModuleQuiz> SkillModuleQuizzes { get; set; }

    public virtual DbSet<SkillModuleQuizAnswer> SkillModuleQuizAnswers { get; set; }

    public virtual DbSet<SkillModuleQuizAttempt> SkillModuleQuizAttempts { get; set; }

    public virtual DbSet<SkillModuleQuizOption> SkillModuleQuizOptions { get; set; }

    public virtual DbSet<SkillModuleQuizQuestion> SkillModuleQuizQuestions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivityStat> UserActivityStats { get; set; }

    public virtual DbSet<UserAiCreditPlan> UserAiCreditPlans { get; set; }

    public virtual DbSet<UserAuthProvider> UserAuthProviders { get; set; }

    public virtual DbSet<UserInsight> UserInsights { get; set; }

    public virtual DbSet<UserNodeProgress> UserNodeProgresses { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("vector");

        modelBuilder.Entity<AiCreditPlan>(entity =>
        {
            entity.HasKey(e => e.PlanCode).HasName("ai_credit_plan_pkey");

            entity.ToTable("ai_credit_plan");

            entity.Property(e => e.PlanCode)
                .HasMaxLength(30)
                .HasColumnName("plan_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DailyCreditLimit).HasColumnName("daily_credit_limit");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MonthlyCreditLimit).HasColumnName("monthly_credit_limit");
        });

        modelBuilder.Entity<AiCreditUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("ai_credit_usage_pkey");

            entity.ToTable("ai_credit_usage");

            entity.HasIndex(e => new { e.FeatureName, e.CreatedAt }, "idx_ai_credit_usage_feature_created_at");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_ai_credit_usage_user_created_at");

            entity.Property(e => e.UsageId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("usage_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreditCost)
                .HasDefaultValue(1)
                .HasColumnName("credit_cost");
            entity.Property(e => e.FeatureName)
                .HasMaxLength(80)
                .HasColumnName("feature_name");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.RequestRefId).HasColumnName("request_ref_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AiCreditUsages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ai_credit_usage_user_id");
        });

        modelBuilder.Entity<AiMentorConversation>(entity =>
        {
            entity.HasKey(e => e.AiMentorConversationId).HasName("ai_mentor_conversation_pkey");

            entity.ToTable("ai_mentor_conversation");

            entity.HasIndex(e => new { e.UserId, e.ArchivedAt }, "ix_ai_mentor_conversation_user_active").HasFilter("(archived_at IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.UpdatedAt }, "ix_ai_mentor_conversation_user_updated_at").IsDescending(false, true);

            entity.Property(e => e.AiMentorConversationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ai_mentor_conversation_id");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PageContext)
                .HasMaxLength(100)
                .HasDefaultValueSql("'roadmap_selection'::character varying")
                .HasColumnName("page_context");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasDefaultValueSql("'New conversation'::character varying")
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AiMentorConversations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ai_mentor_conversation_user");
        });

        modelBuilder.Entity<AiMentorMessage>(entity =>
        {
            entity.HasKey(e => e.AiMentorMessageId).HasName("ai_mentor_message_pkey");

            entity.ToTable("ai_mentor_message");

            entity.HasIndex(e => new { e.AiMentorConversationId, e.CreatedAt }, "ix_ai_mentor_message_conversation_created_at");

            entity.Property(e => e.AiMentorMessageId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ai_mentor_message_id");
            entity.Property(e => e.AiMentorConversationId).HasColumnName("ai_mentor_conversation_id");
            entity.Property(e => e.AiModel)
                .HasMaxLength(100)
                .HasColumnName("ai_model");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasColumnName("role");
            entity.Property(e => e.Sources)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("sources");

            entity.HasOne(d => d.AiMentorConversation).WithMany(p => p.AiMentorMessages)
                .HasForeignKey(d => d.AiMentorConversationId)
                .HasConstraintName("fk_ai_mentor_message_conversation");
        });

        modelBuilder.Entity<CareerRole>(entity =>
        {
            entity.HasKey(e => e.CareerRoleId).HasName("career_role_pkey");

            entity.ToTable("career_role");

            entity.HasIndex(e => e.Name, "career_role_name_key").IsUnique();

            entity.HasIndex(e => e.Slug, "career_role_slug_key").IsUnique();

            entity.HasIndex(e => e.Category, "ix_career_role_category");

            entity.HasIndex(e => e.Slug, "ix_career_role_slug");

            entity.Property(e => e.CareerRoleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("career_role_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(120)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("email_verification_token_pkey");

            entity.ToTable("email_verification_token");

            entity.HasIndex(e => e.PendingLocalRegistrationId, "ix_email_verification_token_pending_local_registration_id");

            entity.Property(e => e.VerificationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("verification_id");
            entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.MaxAttempts)
                .HasDefaultValue(5)
                .HasColumnName("max_attempts");
            entity.Property(e => e.OtpHash).HasColumnName("otp_hash");
            entity.Property(e => e.PendingLocalRegistrationId).HasColumnName("pending_local_registration_id");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.Purpose)
                .HasMaxLength(50)
                .HasColumnName("purpose");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.PendingLocalRegistration).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.PendingLocalRegistrationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_email_verification_pending_local_registration_id");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_email_verification_user_id");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("invoice_pkey");

            entity.ToTable("invoice");

            entity.Property(e => e.InvoiceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("invoice_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValueSql("'VND'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_invoice_user_id");
        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.JobPostingId).HasName("job_posting_pkey");

            entity.ToTable("job_posting");

            entity.HasIndex(e => new { e.IsActive, e.LastSeenAt }, "ix_job_posting_active_last_seen");

            entity.HasIndex(e => e.Category, "ix_job_posting_category");

            entity.HasIndex(e => e.LifecycleStatus, "ix_job_posting_lifecycle_status");

            entity.HasIndex(e => e.PublishedAt, "ix_job_posting_published_at");

            entity.HasIndex(e => new { e.SalaryMin, e.SalaryMax }, "ix_job_posting_salary_range");

            entity.HasIndex(
                e => new { e.ExperienceMinYears, e.ExperienceMaxYears },
                "ix_job_posting_experience_range");

            entity.HasIndex(e => e.ScrapedAt, "ix_job_posting_scraped_at");

            entity.HasIndex(e => e.SourceJobId, "ix_job_posting_source_job_id");

            entity.HasIndex(e => e.Title, "ix_job_posting_title");

            entity.HasIndex(e => e.ExternalId, "uq_job_posting_external_id").IsUnique();

            entity.Property(e => e.JobPostingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_posting_id");
            entity.Property(e => e.Benefits)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("benefits");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.ClosedDetectedAt).HasColumnName("closed_detected_at");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(160)
                .HasColumnName("company_name");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("content_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Experience)
                .HasMaxLength(100)
                .HasColumnName("experience");
            entity.Property(e => e.ExperienceMaxYears).HasColumnName("experience_max_years");
            entity.Property(e => e.ExperienceMinYears).HasColumnName("experience_min_years");
            entity.Property(e => e.ExperienceRaw)
                .HasMaxLength(160)
                .HasColumnName("experience_raw");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(120)
                .HasColumnName("external_id");
            entity.Property(e => e.FirstSeenAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("first_seen_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastChangedAt).HasColumnName("last_changed_at");
            entity.Property(e => e.LastCheckedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_checked_at");
            entity.Property(e => e.LastSeenAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_seen_at");
            entity.Property(e => e.LifecycleStatus)
                .HasMaxLength(32)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("lifecycle_status");
            entity.Property(e => e.Location)
                .HasMaxLength(160)
                .HasColumnName("location");
            entity.Property(e => e.MissingScanCount).HasColumnName("missing_scan_count");
            entity.Property(e => e.PostDateText)
                .HasMaxLength(80)
                .HasColumnName("post_date_text");
            entity.Property(e => e.PostDateConfidence)
                .HasMaxLength(20)
                .HasDefaultValue("unknown")
                .IsRequired()
                .HasColumnName("post_date_confidence");
            entity.Property(e => e.PostDateLowerBound)
                .HasColumnType("date")
                .HasColumnName("post_date_lower_bound");
            entity.Property(e => e.PostDateObservedOn)
                .HasColumnType("date")
                .HasColumnName("post_date_observed_on");
            entity.Property(e => e.PostDateUpperBound)
                .HasColumnType("date")
                .HasColumnName("post_date_upper_bound");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.Requirements)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("requirements");
            entity.Property(e => e.Salary)
                .HasMaxLength(100)
                .HasColumnName("salary");
            entity.Property(e => e.SalaryCurrency)
                .HasMaxLength(16)
                .HasColumnName("salary_currency");
            entity.Property(e => e.SalaryIsNegotiable).HasColumnName("salary_is_negotiable");
            entity.Property(e => e.SalaryMax).HasColumnName("salary_max");
            entity.Property(e => e.SalaryMin).HasColumnName("salary_min");
            entity.Property(e => e.SalaryRaw)
                .HasMaxLength(160)
                .HasColumnName("salary_raw");
            entity.Property(e => e.ScrapedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("scraped_at");
            entity.Property(e => e.SeenCount)
                .HasDefaultValue(1)
                .HasColumnName("seen_count");
            entity.Property(e => e.SourceJobId)
                .HasMaxLength(120)
                .HasColumnName("source_job_id");
            entity.Property(e => e.SourceUpdatedAt).HasColumnName("source_updated_at");
            entity.Property(e => e.DetailLastSuccessAt).HasColumnName("detail_last_success_at");
            entity.Property(e => e.DetailStatus)
                .HasMaxLength(32)
                .HasColumnName("detail_status");
            entity.Property(e => e.Specialties)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("specialties");
            entity.Property(e => e.Skills)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("skills");
            entity.Property(e => e.Title)
                .HasMaxLength(250)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedScanCount).HasColumnName("updated_scan_count");
            entity.Property(e => e.Url).HasColumnName("url");

        });

        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.HasKey(e => e.LearningResourceId).HasName("learning_resource_pkey");

            entity.ToTable("learning_resource");

            entity.HasIndex(e => e.Provider, "ix_learning_resource_provider");

            entity.HasIndex(e => e.ResourceType, "ix_learning_resource_type");

            entity.Property(e => e.LearningResourceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("learning_resource_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(30)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.LanguageCode)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying")
                .HasColumnName("language_code");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .HasColumnName("provider");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(30)
                .HasColumnName("resource_type");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'verified'::character varying")
                .HasColumnName("verification_status");
        });

        modelBuilder.Entity<LearningResourceSkill>(entity =>
        {
            entity.HasKey(e => e.LearningResourceSkillId).HasName("learning_resource_skill_pkey");

            entity.ToTable("learning_resource_skill");

            entity.HasIndex(e => e.LearningResourceId, "ix_learning_resource_skill_resource");

            entity.HasIndex(e => e.SkillId, "ix_learning_resource_skill_skill");

            entity.HasIndex(e => new { e.LearningResourceId, e.SkillId }, "uq_learning_resource_skill").IsUnique();

            entity.Property(e => e.LearningResourceSkillId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("learning_resource_skill_id");
            entity.Property(e => e.LearningResourceId).HasColumnName("learning_resource_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");

            entity.HasOne(d => d.LearningResource).WithMany(p => p.LearningResourceSkills)
                .HasForeignKey(d => d.LearningResourceId)
                .HasConstraintName("fk_learning_resource_skill_resource");

            entity.HasOne(d => d.Skill).WithMany(p => p.LearningResourceSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_learning_resource_skill_skill");
        });

        modelBuilder.Entity<MarketPulseClassifierKeywordMapping>(entity =>
        {
            entity.HasKey(e => e.MarketPulseClassifierKeywordMappingId).HasName("market_pulse_classifier_keyword_mapping_pkey");

            entity.ToTable("market_pulse_classifier_keyword_mapping");

            entity.HasIndex(e => new { e.IsEnabled, e.Category }, "ix_market_pulse_classifier_enabled_category");

            entity.HasIndex(e => new { e.Keyword, e.Category }, "uq_market_pulse_classifier_keyword_category").IsUnique();

            entity.Property(e => e.MarketPulseClassifierKeywordMappingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("market_pulse_classifier_keyword_mapping_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            entity.Property(e => e.Keyword)
                .HasMaxLength(160)
                .HasColumnName("keyword");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Weight)
                .HasPrecision(8, 2)
                .HasDefaultValue(1m)
                .HasColumnName("weight");
        });

        modelBuilder.Entity<MarketPulseCrawlRun>(entity =>
        {
            entity.HasKey(e => e.MarketPulseCrawlRunId).HasName("market_pulse_import_run_pkey");

            entity.ToTable("market_pulse_import_run");

            entity.HasIndex(e => new { e.StartedAt, e.Status }, "ix_market_pulse_import_run_started_status").IsDescending(true, false);

            entity.Property(e => e.MarketPulseCrawlRunId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("market_pulse_import_run_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DuplicateCount).HasColumnName("duplicate_count");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.ErrorSummary).HasColumnName("error_summary");
            entity.Property(e => e.FailedCount).HasColumnName("failed_count");
            entity.Property(e => e.FetchedCount).HasColumnName("fetched_count");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.ImportedCount).HasColumnName("imported_count");
            entity.Property(e => e.IsCompleteSync).HasColumnName("is_complete_sync");
            entity.Property(e => e.LifecycleSkippedReason).HasColumnName("lifecycle_skipped_reason");
            entity.Property(e => e.MissingLifecycleApplied).HasColumnName("missing_lifecycle_applied");
            entity.Property(e => e.Mode)
                .HasMaxLength(40)
                .HasDefaultValueSql("'scheduled'::character varying")
                .HasColumnName("mode");
            entity.Property(e => e.SavedCount).HasColumnName("saved_count");
            entity.Property(e => e.SkippedCount).HasColumnName("skipped_count");
            entity.Property(e => e.SourceGeneratedAt).HasColumnName("source_generated_at");
            entity.Property(e => e.SourceLatestSuccessAt).HasColumnName("source_latest_success_at");
            entity.Property(e => e.SourceTotalCount).HasColumnName("source_total_count");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(40)
                .HasDefaultValueSql("'running'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.StoppedReason).HasColumnName("stopped_reason");
            entity.Property(e => e.TriggerType)
                .HasMaxLength(40)
                .HasDefaultValueSql("'manual'::character varying")
                .HasColumnName("trigger_type");
            entity.Property(e => e.UpdatedCount).HasColumnName("updated_count");
        });

        modelBuilder.Entity<MarketPulseFailedItem>(entity =>
        {
            entity.HasKey(e => e.MarketPulseFailedItemId).HasName("market_pulse_import_failure_pkey");

            entity.ToTable("market_pulse_import_failure");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "ix_market_pulse_import_failure_status_created").IsDescending(false, true);

            entity.Property(e => e.MarketPulseFailedItemId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("market_pulse_import_failure_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorCode)
                .HasMaxLength(80)
                .HasDefaultValueSql("'UNKNOWN'::character varying")
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorDetail).HasColumnName("error_detail");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.LastRetryAt).HasColumnName("last_retry_at");
            entity.Property(e => e.MarketPulseCrawlRunId).HasColumnName("market_pulse_import_run_id");
            entity.Property(e => e.RawPayload)
                .HasColumnType("jsonb")
                .HasColumnName("raw_payload");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.Stage)
                .HasMaxLength(40)
                .HasDefaultValueSql("'unknown'::character varying")
                .HasColumnName("stage");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'open'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");

            entity.HasOne(d => d.MarketPulseCrawlRun).WithMany(p => p.MarketPulseFailedItems)
                .HasForeignKey(d => d.MarketPulseCrawlRunId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_market_pulse_import_failure_run");
        });

        modelBuilder.Entity<MarketPulsePublicationHistoryState>(entity =>
        {
            entity.HasKey(e => e.SingletonId)
                .HasName("market_pulse_publication_history_state_pkey");
            entity.ToTable("market_pulse_publication_history_state");
            entity.Property(e => e.SingletonId).HasColumnName("singleton_id");
            entity.Property(e => e.CoverageStart).HasColumnType("date").HasColumnName("coverage_start");
            entity.Property(e => e.CoverageEnd).HasColumnType("date").HasColumnName("coverage_end");
            entity.Property(e => e.SourceDataAt).HasColumnName("source_data_at");
            entity.Property(e => e.LastSuccessfulSyncAt).HasColumnName("last_successful_sync_at");
            entity.Property(e => e.SyncedPostingCount).HasColumnName("synced_posting_count");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<MarketPulseRefreshOperation>(entity =>
        {
            entity.HasKey(e => e.MarketPulseRefreshOperationId)
                .HasName("market_pulse_refresh_operation_pkey");
            entity.ToTable("market_pulse_refresh_operation");
            entity.HasIndex(e => e.RequestedAt, "ix_market_pulse_refresh_operation_requested_at")
                .IsDescending();
            // The database owns uq_market_pulse_refresh_operation_active as a partial
            // expression index on the constant (1), ensuring exactly one active row.
            entity.Property(e => e.MarketPulseRefreshOperationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("market_pulse_refresh_operation_id");
            entity.Property(e => e.Status).HasMaxLength(24).HasColumnName("status");
            entity.Property(e => e.BaselineCrawlerSuccessAt).HasColumnName("baseline_crawler_success_at");
            entity.Property(e => e.CrawlerSuccessAt).HasColumnName("crawler_success_at");
            entity.Property(e => e.ImportRunId).HasColumnName("market_pulse_import_run_id");
            entity.Property(e => e.CurrentStep).HasMaxLength(24).HasColumnName("current_step");
            entity.Property(e => e.TriggerType).HasMaxLength(24).HasColumnName("trigger_type");
            entity.Property(e => e.ErrorCode).HasMaxLength(80).HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.RequestedAt).HasColumnName("requested_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("payment_transaction_pkey");

            entity.ToTable("payment_transaction");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Gateway)
                .HasMaxLength(30)
                .HasColumnName("gateway");
            entity.Property(e => e.GatewayTransactionId)
                .HasMaxLength(100)
                .HasColumnName("gateway_transaction_id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.WebhookPayload)
                .HasColumnType("jsonb")
                .HasColumnName("webhook_payload");

            entity.HasOne(d => d.Invoice).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("fk_payment_transaction_invoice_id");
        });

        modelBuilder.Entity<PendingLocalRegistration>(entity =>
        {
            entity.HasKey(e => e.PendingLocalRegistrationId).HasName("pending_local_registration_pkey");

            entity.ToTable("pending_local_registration");

            entity.HasIndex(e => e.ExpiresAt, "ix_pending_local_registration_expires_at").HasFilter("(used_at IS NULL)");

            entity.HasIndex(e => e.Email, "uq_pending_local_registration_email_active")
                .IsUnique()
                .HasFilter("(used_at IS NULL)");

            entity.Property(e => e.PendingLocalRegistrationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("pending_local_registration_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
            entity.Property(e => e.UsernameNormalized)
                .HasMaxLength(50)
                .HasColumnName("username_normalized");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("permission_pkey");

            entity.ToTable("permission");

            entity.HasIndex(e => e.PermissionName, "permission_permission_name_key").IsUnique();

            entity.Property(e => e.PermissionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("permission_id");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(100)
                .HasColumnName("permission_name");
        });

        modelBuilder.Entity<PermissionRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permission_role_pkey");

            entity.ToTable("permission_role");

            entity.HasIndex(e => new { e.PermissionId, e.RoleId }, "uq_permission_role").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.PermissionRoles)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("fk_permission_role_permission_id");

            entity.HasOne(d => d.Role).WithMany(p => p.PermissionRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_permission_role_role_id");
        });

        modelBuilder.Entity<ProgressEvent>(entity =>
        {
            entity.HasKey(e => e.ProgressEventId).HasName("progress_event_pkey");

            entity.ToTable("progress_event");

            entity.HasIndex(e => e.RoadmapEnrollmentId, "ix_progress_event_enrollment_id");

            entity.HasIndex(e => e.UserId, "ix_progress_event_user_id");

            entity.HasIndex(e => new { e.RoadmapEnrollmentId, e.IdempotencyKey }, "uq_progress_event_idempotency_not_null")
                .IsUnique()
                .HasFilter("(idempotency_key IS NOT NULL)");

            entity.Property(e => e.ProgressEventId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("progress_event_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IdempotencyKey)
                .HasMaxLength(100)
                .HasColumnName("idempotency_key");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(30)
                .HasColumnName("new_status");
            entity.Property(e => e.OldStatus)
                .HasMaxLength(30)
                .HasColumnName("old_status");
            entity.Property(e => e.RoadmapEnrollmentId).HasColumnName("roadmap_enrollment_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.RoadmapEnrollment).WithMany(p => p.ProgressEvents)
                .HasForeignKey(d => d.RoadmapEnrollmentId)
                .HasConstraintName("fk_progress_event_enrollment");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.ProgressEvents)
                .HasForeignKey(d => d.RoadmapNodeId)
                .HasConstraintName("fk_progress_event_node");

            entity.HasOne(d => d.User).WithMany(p => p.ProgressEvents)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_progress_event_user");
        });

        modelBuilder.Entity<RepoInsight>(entity =>
        {
            entity.HasKey(e => e.InsightId).HasName("repo_insight_pkey");

            entity.ToTable("repo_insight");

            entity.HasIndex(e => e.RepositoryId, "uq_repo_insight_repository_id").IsUnique();

            entity.Property(e => e.InsightId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("insight_id");
            entity.Property(e => e.AiModel)
                .HasMaxLength(100)
                .HasColumnName("ai_model");
            entity.Property(e => e.AnalysisStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'completed'::character varying")
                .HasColumnName("analysis_status");
            entity.Property(e => e.AnalyzedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("analyzed_at");
            entity.Property(e => e.DetectedSkills)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("detected_skills");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ProjectType)
                .HasMaxLength(100)
                .HasColumnName("project_type");
            entity.Property(e => e.ReadmeHash)
                .HasMaxLength(64)
                .HasColumnName("readme_hash");
            entity.Property(e => e.ReadmeTruncated).HasColumnName("readme_truncated");
            entity.Property(e => e.RepositoryId).HasColumnName("repository_id");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.TechStack)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("tech_stack");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Repository).WithOne(p => p.RepoInsight)
                .HasForeignKey<RepoInsight>(d => d.RepositoryId)
                .HasConstraintName("fk_repo_insight_repository_id");
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.RepositoryId).HasName("repository_pkey");

            entity.ToTable("repository");

            entity.HasIndex(e => new { e.UserId, e.GithubRepoId }, "uq_user_github_repo").IsUnique();

            entity.Property(e => e.RepositoryId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("repository_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Forks).HasColumnName("forks");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.GithubCreatedAt).HasColumnName("github_created_at");
            entity.Property(e => e.GithubRepoId).HasColumnName("github_repo_id");
            entity.Property(e => e.GithubUpdatedAt).HasColumnName("github_updated_at");
            entity.Property(e => e.HtmlUrl).HasColumnName("html_url");
            entity.Property(e => e.IsPrivate).HasColumnName("is_private");
            entity.Property(e => e.IsSelectedForPortfolio)
                .HasDefaultValue(true)
                .HasColumnName("is_selected_for_portfolio");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.PrimaryLanguage)
                .HasMaxLength(50)
                .HasColumnName("primary_language");
            entity.Property(e => e.Stars).HasColumnName("stars");
            entity.Property(e => e.SyncedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("synced_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Repositories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_repository_user_id");
        });

        modelBuilder.Entity<Roadmap>(entity =>
        {
            entity.HasKey(e => e.RoadmapId).HasName("roadmap_pkey");

            entity.ToTable("roadmap");

            entity.HasIndex(e => e.CareerRoleId, "ix_roadmap_career_role_id");

            entity.HasIndex(e => e.OwnerUserId, "ix_roadmap_owner_user_id");

            entity.HasIndex(e => e.Slug, "uq_roadmap_slug").IsUnique();

            entity.Property(e => e.RoadmapId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_id");
            entity.Property(e => e.CareerRoleId).HasColumnName("career_role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Visibility)
                .HasMaxLength(30)
                .HasDefaultValueSql("'public'::character varying")
                .HasColumnName("visibility");

            entity.HasOne(d => d.CareerRole).WithMany(p => p.Roadmaps)
                .HasForeignKey(d => d.CareerRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_roadmap_career_role");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Roadmaps)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_roadmap_owner_user");
        });

        modelBuilder.Entity<RoadmapEdge>(entity =>
        {
            entity.HasKey(e => e.RoadmapEdgeId).HasName("roadmap_edge_pkey");

            entity.ToTable("roadmap_edge");

            entity.HasIndex(e => e.FromNodeId, "ix_roadmap_edge_from_node");

            entity.HasIndex(e => e.ToNodeId, "ix_roadmap_edge_to_node");

            entity.HasIndex(e => e.EdgeType, "ix_roadmap_edge_type");

            entity.HasIndex(e => e.RoadmapVersionId, "ix_roadmap_edge_version_id");

            entity.HasIndex(e => new { e.RoadmapVersionId, e.FromNodeId, e.ToNodeId, e.EdgeType }, "uq_roadmap_edge").IsUnique();

            entity.Property(e => e.RoadmapEdgeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_edge_id");
            entity.Property(e => e.Condition)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("condition");
            entity.Property(e => e.DependencyType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'required'::character varying")
                .HasColumnName("dependency_type");
            entity.Property(e => e.EdgeType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'dependency'::character varying")
                .HasColumnName("edge_type");
            entity.Property(e => e.FromNodeId).HasColumnName("from_node_id");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");
            entity.Property(e => e.ToNodeId).HasColumnName("to_node_id");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.RoadmapEdges)
                .HasForeignKey(d => d.RoadmapVersionId)
                .HasConstraintName("fk_roadmap_edge_version");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.RoadmapEdgeRoadmapNodes)
                .HasPrincipalKey(p => new { p.RoadmapVersionId, p.RoadmapNodeId })
                .HasForeignKey(d => new { d.RoadmapVersionId, d.FromNodeId })
                .HasConstraintName("fk_roadmap_edge_from_node");

            entity.HasOne(d => d.RoadmapNodeNavigation).WithMany(p => p.RoadmapEdgeRoadmapNodeNavigations)
                .HasPrincipalKey(p => new { p.RoadmapVersionId, p.RoadmapNodeId })
                .HasForeignKey(d => new { d.RoadmapVersionId, d.ToNodeId })
                .HasConstraintName("fk_roadmap_edge_to_node");
        });

        modelBuilder.Entity<RoadmapEnrollment>(entity =>
        {
            entity.HasKey(e => e.RoadmapEnrollmentId).HasName("roadmap_enrollment_pkey");

            entity.ToTable("roadmap_enrollment");

            entity.HasIndex(e => e.UserId, "ix_roadmap_enrollment_user_id");

            entity.HasIndex(e => e.RoadmapVersionId, "ix_roadmap_enrollment_version_id");

            entity.HasIndex(e => new { e.UserId, e.RoadmapVersionId }, "uq_user_roadmap_version_enrollment").IsUnique();

            entity.Property(e => e.RoadmapEnrollmentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_enrollment_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ProgressPercent)
                .HasPrecision(5, 2)
                .HasColumnName("progress_percent");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.RoadmapEnrollments)
                .HasForeignKey(d => d.RoadmapVersionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_roadmap_enrollment_version");

            entity.HasOne(d => d.User).WithMany(p => p.RoadmapEnrollments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_roadmap_enrollment_user");
        });

        modelBuilder.Entity<RoadmapNode>(entity =>
        {
            entity.HasKey(e => e.RoadmapNodeId).HasName("roadmap_node_pkey");

            entity.ToTable("roadmap_node");

            entity.HasIndex(e => e.CheckpointType, "ix_roadmap_node_checkpoint_type");

            entity.HasIndex(e => e.LayoutRole, "ix_roadmap_node_layout_role");

            entity.HasIndex(e => e.ParentNodeId, "ix_roadmap_node_parent_id");

            entity.HasIndex(e => new { e.RoadmapVersionId, e.Slug }, "ix_roadmap_node_slug");

            entity.HasIndex(e => e.NodeType, "ix_roadmap_node_type");

            entity.HasIndex(e => e.RoadmapVersionId, "ix_roadmap_node_version_id");

            entity.HasIndex(e => new { e.RoadmapVersionId, e.Slug }, "uq_roadmap_node_version_slug").IsUnique();

            entity.HasIndex(e => new { e.RoadmapVersionId, e.RoadmapNodeId }, "uq_roadmap_version_node").IsUnique();

            entity.Property(e => e.RoadmapNodeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_node_id");
            entity.Property(e => e.CheckpointType)
                .HasMaxLength(30)
                .HasColumnName("checkpoint_type");
            entity.Property(e => e.CompletionCriteria)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("completion_criteria");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(30)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.EstimatedHours).HasColumnName("estimated_hours");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("is_required");
            entity.Property(e => e.IsTrackable)
                .HasDefaultValue(true)
                .HasColumnName("is_trackable");
            entity.Property(e => e.LayoutRole)
                .HasMaxLength(30)
                .HasDefaultValueSql("'side'::character varying")
                .HasColumnName("layout_role");
            entity.Property(e => e.LearningOutcomes)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("learning_outcomes");
            entity.Property(e => e.Metadata)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.NodeType)
                .HasMaxLength(30)
                .HasColumnName("node_type");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.ParentNodeId).HasColumnName("parent_node_id");
            entity.Property(e => e.RequiredCount).HasColumnName("required_count");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");
            entity.Property(e => e.SelectionType)
                .HasMaxLength(30)
                .HasColumnName("selection_type");
            entity.Property(e => e.Slug)
                .HasMaxLength(120)
                .HasColumnName("slug");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.ParentNode).WithMany(p => p.InverseParentNode)
                .HasForeignKey(d => d.ParentNodeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_roadmap_node_parent");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.RoadmapNodes)
                .HasForeignKey(d => d.RoadmapVersionId)
                .HasConstraintName("fk_roadmap_node_version");
        });

        modelBuilder.Entity<RoadmapNodeResource>(entity =>
        {
            entity.HasKey(e => e.RoadmapNodeResourceId).HasName("roadmap_node_resource_pkey");

            entity.ToTable("roadmap_node_resource");

            entity.HasIndex(e => e.RoadmapNodeId, "ix_roadmap_node_resource_node_id");

            entity.HasIndex(e => e.LearningResourceId, "ix_roadmap_node_resource_resource_id");

            entity.HasIndex(e => new { e.RoadmapNodeId, e.LearningResourceId }, "uq_roadmap_node_resource").IsUnique();

            entity.Property(e => e.RoadmapNodeResourceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_node_resource_id");
            entity.Property(e => e.LearningResourceId).HasColumnName("learning_resource_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");

            entity.HasOne(d => d.LearningResource).WithMany(p => p.RoadmapNodeResources)
                .HasForeignKey(d => d.LearningResourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_roadmap_node_resource_resource");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.RoadmapNodeResources)
                .HasForeignKey(d => d.RoadmapNodeId)
                .HasConstraintName("fk_roadmap_node_resource_node");
        });

        modelBuilder.Entity<RoadmapNodeSkill>(entity =>
        {
            entity.HasKey(e => e.RoadmapNodeSkillId).HasName("roadmap_node_skill_pkey");

            entity.ToTable("roadmap_node_skill");

            entity.HasIndex(e => e.RoadmapNodeId, "ix_roadmap_node_skill_node");

            entity.HasIndex(e => e.SkillId, "ix_roadmap_node_skill_skill");

            entity.HasIndex(e => new { e.RoadmapNodeId, e.SkillId }, "uq_roadmap_node_skill").IsUnique();

            entity.Property(e => e.RoadmapNodeSkillId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_node_skill_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.RoadmapNodeSkills)
                .HasForeignKey(d => d.RoadmapNodeId)
                .HasConstraintName("fk_roadmap_node_skill_node");

            entity.HasOne(d => d.Skill).WithMany(p => p.RoadmapNodeSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_roadmap_node_skill_skill");
        });

        modelBuilder.Entity<RoadmapVersion>(entity =>
        {
            entity.HasKey(e => e.RoadmapVersionId).HasName("roadmap_version_pkey");

            entity.ToTable("roadmap_version");

            entity.HasIndex(e => e.CreatedByUserId, "ix_roadmap_version_created_by_user_id");

            entity.HasIndex(e => e.CreatedFromVersionId, "ix_roadmap_version_created_from_version_id");

            entity.HasIndex(e => new { e.Status, e.UpdatedAt }, "ix_roadmap_version_review_queue")
                .IsDescending(false, true)
                .HasFilter("((status)::text = ANY ((ARRAY['pending_review'::character varying, 'changes_requested'::character varying])::text[]))");

            entity.HasIndex(e => e.RoadmapId, "ix_roadmap_version_roadmap_id");

            entity.HasIndex(e => e.Status, "ix_roadmap_version_status");

            entity.HasIndex(e => new { e.RoadmapId, e.VersionNumber }, "uq_roadmap_version_number").IsUnique();

            entity.HasIndex(e => new { e.RoadmapId, e.MajorVersion, e.MinorVersion, e.PatchVersion }, "uq_roadmap_version_semver").IsUnique();

            entity.Property(e => e.RoadmapVersionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_version_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedFromVersionId).HasColumnName("created_from_version_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedTotalHours).HasColumnName("estimated_total_hours");
            entity.Property(e => e.LayoutAlgorithm)
                .HasMaxLength(50)
                .HasColumnName("layout_algorithm");
            entity.Property(e => e.LayoutDirection)
                .HasMaxLength(20)
                .HasDefaultValueSql("'TB'::character varying")
                .HasColumnName("layout_direction");
            entity.Property(e => e.MajorVersion)
                .HasDefaultValue(1)
                .HasColumnName("major_version");
            entity.Property(e => e.MinorVersion).HasColumnName("minor_version");
            entity.Property(e => e.PatchVersion).HasColumnName("patch_version");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ReleaseType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'initial'::character varying")
                .HasColumnName("release_type");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'draft'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.RoadmapVersions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_roadmap_version_created_by_user");

            entity.HasOne(d => d.CreatedFromVersion).WithMany(p => p.InverseCreatedFromVersion)
                .HasForeignKey(d => d.CreatedFromVersionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_roadmap_version_created_from_version");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.RoadmapVersions)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("fk_roadmap_version_roadmap");
        });

        modelBuilder.Entity<RoadmapVersionReviewEvent>(entity =>
        {
            entity.HasKey(e => e.RoadmapVersionReviewEventId).HasName("roadmap_version_review_event_pkey");

            entity.ToTable("roadmap_version_review_event");

            entity.HasIndex(e => e.ActorUserId, "ix_roadmap_version_review_event_actor_user_id");

            entity.HasIndex(e => e.RoadmapVersionId, "ix_roadmap_version_review_event_version_id");

            entity.Property(e => e.RoadmapVersionReviewEventId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_version_review_event_id");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EventType)
                .HasMaxLength(30)
                .HasColumnName("event_type");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.RoadmapVersionReviewEvents)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_roadmap_review_event_actor_user");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.RoadmapVersionReviewEvents)
                .HasForeignKey(d => d.RoadmapVersionId)
                .HasConstraintName("fk_roadmap_review_event_version");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleName, "role_role_name_key").IsUnique();

            entity.Property(e => e.RoleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("skill_pkey");

            entity.ToTable("skill");

            entity.HasIndex(e => e.Category, "ix_skill_category");

            entity.HasIndex(e => e.Slug, "ix_skill_slug");

            entity.HasIndex(e => e.Name, "uq_skill_name").IsUnique();

            entity.HasIndex(e => e.Slug, "uq_skill_slug").IsUnique();

            entity.Property(e => e.SkillId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(120)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SkillGapAnalysisHistory>(entity =>
        {
            entity.HasKey(e => e.SkillGapAnalysisHistoryId).HasName("skill_gap_analysis_history_pkey");

            entity.ToTable("skill_gap_analysis_history");

            entity.Property(e => e.SkillGapAnalysisHistoryId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_gap_analysis_history_id");
            entity.Property(e => e.AuthorNameSnapshot)
                .HasMaxLength(255)
                .HasColumnName("author_name_snapshot");
            entity.Property(e => e.CareerRoleId).HasColumnName("career_role_id");
            entity.Property(e => e.CareerRoleNameSnapshot)
                .HasMaxLength(255)
                .HasColumnName("career_role_name_snapshot");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.MatchedSkills).HasColumnName("matched_skills");
            entity.Property(e => e.MissingSkills).HasColumnName("missing_skills");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.RoadmapTitleSnapshot)
                .HasMaxLength(255)
                .HasColumnName("roadmap_title_snapshot");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");
            entity.Property(e => e.RoadmapVersionTitleSnapshot)
                .HasMaxLength(255)
                .HasColumnName("roadmap_version_title_snapshot");
            entity.Property(e => e.SnapshotJson)
                .HasColumnType("jsonb")
                .HasColumnName("snapshot_json");
            entity.Property(e => e.TotalSkills).HasColumnName("total_skills");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CareerRole).WithMany(p => p.SkillGapAnalysisHistories)
                .HasForeignKey(d => d.CareerRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("skill_gap_analysis_history_career_role_id_fkey");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.SkillGapAnalysisHistories)
                .HasForeignKey(d => d.RoadmapId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("skill_gap_analysis_history_roadmap_id_fkey");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.SkillGapAnalysisHistories)
                .HasForeignKey(d => d.RoadmapVersionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("skill_gap_analysis_history_roadmap_version_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.SkillGapAnalysisHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("skill_gap_analysis_history_user_id_fkey");
        });

        modelBuilder.Entity<SkillGapCategoryConfig>(entity =>
        {
            entity.HasKey(e => e.SkillGapCategoryConfigId).HasName("skill_gap_category_config_pkey");

            entity.ToTable("skill_gap_category_config");

            entity.HasIndex(e => new { e.RoadmapId, e.RoadmapVersionId, e.CategoryName }, "uq_skill_gap_category").IsUnique();

            entity.Property(e => e.SkillGapCategoryConfigId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_gap_category_config_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.RoadmapVersionId).HasColumnName("roadmap_version_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.SkillGapCategoryConfigs)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("skill_gap_category_config_roadmap_id_fkey");

            entity.HasOne(d => d.RoadmapVersion).WithMany(p => p.SkillGapCategoryConfigs)
                .HasForeignKey(d => d.RoadmapVersionId)
                .HasConstraintName("skill_gap_category_config_roadmap_version_id_fkey");
        });

        modelBuilder.Entity<SkillModule>(entity =>
        {
            entity.HasKey(e => e.SkillModuleId).HasName("skill_module_pkey");

            entity.ToTable("skill_module");

            entity.HasIndex(e => e.CreatedByUserId, "ix_skill_module_created_by_user_id");

            entity.HasIndex(e => e.PublishedAt, "ix_skill_module_published_at").IsDescending();

            entity.HasIndex(e => e.SkillId, "ix_skill_module_skill_id");

            entity.HasIndex(e => new { e.Status, e.UpdatedAt }, "ix_skill_module_status_updated_at").IsDescending(false, true);

            entity.HasIndex(e => e.Slug, "uq_skill_module_slug").IsUnique();

            entity.Property(e => e.SkillModuleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_id");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(30)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.EstimatedHours)
                .HasPrecision(5, 2)
                .HasColumnName("estimated_hours");
            entity.Property(e => e.Metadata)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .HasColumnName("slug");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'draft'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.SkillModules)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_skill_module_created_by_user");

            entity.HasOne(d => d.Skill).WithMany(p => p.SkillModules)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_skill_module_skill");
        });

        modelBuilder.Entity<SkillModuleChunk>(entity =>
        {
            entity.HasKey(e => e.SkillModuleChunkId).HasName("skill_module_chunk_pkey");

            entity.ToTable("skill_module_chunk");

            entity.HasIndex(e => e.SkillModuleLessonId, "ix_skill_module_chunk_lesson_id");

            entity.HasIndex(e => e.SkillModuleId, "ix_skill_module_chunk_module_id");

            entity.HasIndex(e => new { e.SkillModuleLessonId, e.ChunkIndex }, "uq_skill_module_chunk_lesson_index").IsUnique();

            entity.Property(e => e.SkillModuleChunkId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_chunk_id");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ContentHash).HasColumnName("content_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Embedding)
                .HasMaxLength(3072)
                .HasColumnName("embedding");
            entity.Property(e => e.Heading).HasColumnName("heading");
            entity.Property(e => e.SkillModuleId).HasColumnName("skill_module_id");
            entity.Property(e => e.SkillModuleLessonId).HasColumnName("skill_module_lesson_id");
            entity.Property(e => e.TokenCount).HasColumnName("token_count");

            entity.HasOne(d => d.SkillModule).WithMany(p => p.SkillModuleChunks)
                .HasForeignKey(d => d.SkillModuleId)
                .HasConstraintName("fk_skill_module_chunk_module");

            entity.HasOne(d => d.SkillModuleLesson).WithMany(p => p.SkillModuleChunks)
                .HasForeignKey(d => d.SkillModuleLessonId)
                .HasConstraintName("fk_skill_module_chunk_lesson");
        });

        modelBuilder.Entity<SkillModuleEnrollment>(entity =>
        {
            entity.HasKey(e => e.SkillModuleEnrollmentId).HasName("skill_module_enrollment_pkey");

            entity.ToTable("skill_module_enrollment");

            entity.HasIndex(e => e.SkillModuleId, "ix_skill_module_enrollment_module_id");

            entity.HasIndex(e => e.UserId, "ix_skill_module_enrollment_user_id");

            entity.HasIndex(e => new { e.UserId, e.SkillModuleId }, "uq_skill_module_enrollment_user_module").IsUnique();

            entity.Property(e => e.SkillModuleEnrollmentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_enrollment_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LastAccessedLessonId).HasColumnName("last_accessed_lesson_id");
            entity.Property(e => e.LessonProgress)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("lesson_progress");
            entity.Property(e => e.ProgressPercent)
                .HasPrecision(5, 2)
                .HasColumnName("progress_percent");
            entity.Property(e => e.SkillModuleId).HasColumnName("skill_module_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'in_progress'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.LastAccessedLesson).WithMany(p => p.SkillModuleEnrollments)
                .HasForeignKey(d => d.LastAccessedLessonId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_skill_module_enrollment_last_lesson");

            entity.HasOne(d => d.SkillModule).WithMany(p => p.SkillModuleEnrollments)
                .HasForeignKey(d => d.SkillModuleId)
                .HasConstraintName("fk_skill_module_enrollment_module");

            entity.HasOne(d => d.User).WithMany(p => p.SkillModuleEnrollments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_skill_module_enrollment_user");
        });

        modelBuilder.Entity<SkillModuleLesson>(entity =>
        {
            entity.HasKey(e => e.SkillModuleLessonId).HasName("skill_module_lesson_pkey");

            entity.ToTable("skill_module_lesson");

            entity.HasIndex(e => new { e.SkillModuleId, e.IndexingStatus }, "ix_skill_module_lesson_indexing_status");

            entity.HasIndex(e => new { e.SkillModuleId, e.OrderIndex }, "ix_skill_module_lesson_module_order");

            entity.HasIndex(e => new { e.SkillModuleId, e.OrderIndex }, "uq_skill_module_lesson_order").IsUnique();

            entity.HasIndex(e => new { e.SkillModuleId, e.Slug }, "uq_skill_module_lesson_slug").IsUnique();

            entity.Property(e => e.SkillModuleLessonId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_lesson_id");
            entity.Property(e => e.ContentHash).HasColumnName("content_hash");
            entity.Property(e => e.ContentSizeBytes).HasColumnName("content_size_bytes");
            entity.Property(e => e.ContentVersion)
                .HasDefaultValue(1)
                .HasColumnName("content_version");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EstimatedHours)
                .HasPrecision(5, 2)
                .HasColumnName("estimated_hours");
            entity.Property(e => e.IndexedAt).HasColumnName("indexed_at");
            entity.Property(e => e.IndexingError).HasColumnName("indexing_error");
            entity.Property(e => e.IndexingStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("indexing_status");
            entity.Property(e => e.MarkdownFileKey).HasColumnName("markdown_file_key");
            entity.Property(e => e.MarkdownFileName)
                .HasMaxLength(255)
                .HasColumnName("markdown_file_name");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.SkillModuleId).HasColumnName("skill_module_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .HasColumnName("slug");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SkillModule).WithMany(p => p.SkillModuleLessons)
                .HasForeignKey(d => d.SkillModuleId)
                .HasConstraintName("fk_skill_module_lesson_module");
        });

        modelBuilder.Entity<SkillModuleQuiz>(entity =>
        {
            entity.HasKey(e => e.SkillModuleQuizId).HasName("skill_module_quiz_pkey");

            entity.ToTable("skill_module_quiz");

            entity.HasIndex(e => e.Status, "ix_skill_module_quiz_status");

            entity.HasIndex(e => e.SkillModuleId, "uq_skill_module_quiz_module").IsUnique();

            entity.Property(e => e.SkillModuleQuizId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_quiz_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
            entity.Property(e => e.PassingScorePercent)
                .HasPrecision(5, 2)
                .HasDefaultValue(70m)
                .HasColumnName("passing_score_percent");
            entity.Property(e => e.SkillModuleId).HasColumnName("skill_module_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'draft'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SkillModule).WithOne(p => p.SkillModuleQuiz)
                .HasForeignKey<SkillModuleQuiz>(d => d.SkillModuleId)
                .HasConstraintName("fk_skill_module_quiz_module");
        });

        modelBuilder.Entity<SkillModuleQuizAnswer>(entity =>
        {
            entity.HasKey(e => e.SkillModuleQuizAnswerId).HasName("skill_module_quiz_answer_pkey");

            entity.ToTable("skill_module_quiz_answer");

            entity.HasIndex(e => e.SkillModuleQuizAttemptId, "ix_skill_module_answer_attempt_id");

            entity.HasIndex(e => new { e.SkillModuleQuizAttemptId, e.SkillModuleQuizQuestionId }, "uq_skill_module_quiz_answer_question").IsUnique();

            entity.Property(e => e.SkillModuleQuizAnswerId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_quiz_answer_id");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("answered_at");
            entity.Property(e => e.EarnedPoints).HasColumnName("earned_points");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.SelectedOptionId).HasColumnName("selected_option_id");
            entity.Property(e => e.SkillModuleQuizAttemptId).HasColumnName("skill_module_quiz_attempt_id");
            entity.Property(e => e.SkillModuleQuizQuestionId).HasColumnName("skill_module_quiz_question_id");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.SkillModuleQuizAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_skill_module_quiz_answer_option");

            entity.HasOne(d => d.SkillModuleQuizAttempt).WithMany(p => p.SkillModuleQuizAnswers)
                .HasForeignKey(d => d.SkillModuleQuizAttemptId)
                .HasConstraintName("fk_skill_module_quiz_answer_attempt");

            entity.HasOne(d => d.SkillModuleQuizQuestion).WithMany(p => p.SkillModuleQuizAnswers)
                .HasForeignKey(d => d.SkillModuleQuizQuestionId)
                .HasConstraintName("fk_skill_module_quiz_answer_question");
        });

        modelBuilder.Entity<SkillModuleQuizAttempt>(entity =>
        {
            entity.HasKey(e => e.SkillModuleQuizAttemptId).HasName("skill_module_quiz_attempt_pkey");

            entity.ToTable("skill_module_quiz_attempt");

            entity.HasIndex(e => e.SkillModuleEnrollmentId, "ix_skill_module_attempt_enrollment_id");

            entity.HasIndex(e => e.UserId, "ix_skill_module_attempt_user_id");

            entity.HasIndex(e => new { e.SkillModuleQuizId, e.UserId, e.AttemptNo }, "uq_skill_module_quiz_attempt_no").IsUnique();

            entity.Property(e => e.SkillModuleQuizAttemptId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_quiz_attempt_id");
            entity.Property(e => e.AttemptNo).HasColumnName("attempt_no");
            entity.Property(e => e.EarnedPoints).HasColumnName("earned_points");
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.ScorePercent)
                .HasPrecision(5, 2)
                .HasColumnName("score_percent");
            entity.Property(e => e.SkillModuleEnrollmentId).HasColumnName("skill_module_enrollment_id");
            entity.Property(e => e.SkillModuleQuizId).HasColumnName("skill_module_quiz_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'in_progress'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.TotalPoints).HasColumnName("total_points");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.SkillModuleEnrollment).WithMany(p => p.SkillModuleQuizAttempts)
                .HasForeignKey(d => d.SkillModuleEnrollmentId)
                .HasConstraintName("fk_skill_module_quiz_attempt_enrollment");

            entity.HasOne(d => d.SkillModuleQuiz).WithMany(p => p.SkillModuleQuizAttempts)
                .HasForeignKey(d => d.SkillModuleQuizId)
                .HasConstraintName("fk_skill_module_quiz_attempt_quiz");

            entity.HasOne(d => d.User).WithMany(p => p.SkillModuleQuizAttempts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_skill_module_quiz_attempt_user");
        });

        modelBuilder.Entity<SkillModuleQuizOption>(entity =>
        {
            entity.HasKey(e => e.SkillModuleQuizOptionId).HasName("skill_module_quiz_option_pkey");

            entity.ToTable("skill_module_quiz_option");

            entity.HasIndex(e => new { e.SkillModuleQuizQuestionId, e.OrderIndex }, "ix_skill_module_option_question_order");

            entity.HasIndex(e => new { e.SkillModuleQuizQuestionId, e.OrderIndex }, "uq_skill_module_quiz_option_order").IsUnique();

            entity.Property(e => e.SkillModuleQuizOptionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_quiz_option_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.OptionText).HasColumnName("option_text");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.SkillModuleQuizQuestionId).HasColumnName("skill_module_quiz_question_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SkillModuleQuizQuestion).WithMany(p => p.SkillModuleQuizOptions)
                .HasForeignKey(d => d.SkillModuleQuizQuestionId)
                .HasConstraintName("fk_skill_module_quiz_option_question");
        });

        modelBuilder.Entity<SkillModuleQuizQuestion>(entity =>
        {
            entity.HasKey(e => e.SkillModuleQuizQuestionId).HasName("skill_module_quiz_question_pkey");

            entity.ToTable("skill_module_quiz_question");

            entity.HasIndex(e => new { e.SkillModuleQuizId, e.OrderIndex }, "ix_skill_module_question_quiz_order");

            entity.HasIndex(e => new { e.SkillModuleQuizId, e.OrderIndex }, "uq_skill_module_quiz_question_order").IsUnique();

            entity.Property(e => e.SkillModuleQuizQuestionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_module_quiz_question_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.Points)
                .HasDefaultValue(1)
                .HasColumnName("points");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'single_choice'::character varying")
                .HasColumnName("question_type");
            entity.Property(e => e.SkillModuleQuizId).HasColumnName("skill_module_quiz_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SkillModuleQuiz).WithMany(p => p.SkillModuleQuizQuestions)
                .HasForeignKey(d => d.SkillModuleQuizId)
                .HasConstraintName("fk_skill_module_quiz_question_quiz");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pkey");

            entity.ToTable("user");

            entity.HasIndex(e => e.Username, "user_username_key").IsUnique();

            entity.HasIndex(e => e.UsernameNormalized, "user_username_normalized_key").IsUnique();

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
            entity.Property(e => e.UsernameNormalized)
                .HasMaxLength(50)
                .HasColumnName("username_normalized");
        });

        modelBuilder.Entity<UserActivityStat>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_activity_stats_pkey");

            entity.ToTable("user_activity_stats");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.LastInteraction).HasColumnName("last_interaction");
            entity.Property(e => e.LongestStreak).HasColumnName("longest_streak");

            entity.HasOne(d => d.User).WithOne(p => p.UserActivityStat)
                .HasForeignKey<UserActivityStat>(d => d.UserId)
                .HasConstraintName("fk_user_activity_stats_user_id");
        });

        modelBuilder.Entity<UserAiCreditPlan>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_ai_credit_plan_pkey");

            entity.ToTable("user_ai_credit_plan");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PlanCode)
                .HasMaxLength(30)
                .HasColumnName("plan_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.PlanCodeNavigation).WithMany(p => p.UserAiCreditPlans)
                .HasForeignKey(d => d.PlanCode)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_ai_credit_plan_plan_code");

            entity.HasOne(d => d.User).WithOne(p => p.UserAiCreditPlan)
                .HasForeignKey<UserAiCreditPlan>(d => d.UserId)
                .HasConstraintName("fk_user_ai_credit_plan_user_id");
        });

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_auth_provider_pkey");

            entity.ToTable("user_auth_provider");

            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }, "uq_provider_identity").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.Provider }, "uq_user_provider").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PendingEmail)
                .HasMaxLength(254)
                .HasColumnName("pending_email");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderUserId)
                .HasMaxLength(255)
                .HasColumnName("provider_user_id");
            entity.Property(e => e.ProviderUsername)
                .HasMaxLength(50)
                .HasColumnName("provider_username");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserAuthProviders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_auth_provider_user_id");
        });

        modelBuilder.Entity<UserInsight>(entity =>
        {
            entity.HasKey(e => e.InsightId).HasName("user_insight_pkey");

            entity.ToTable("user_insight");

            entity.Property(e => e.InsightId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("insight_id");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserInsights)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_insight_user_id");
        });

        modelBuilder.Entity<UserNodeProgress>(entity =>
        {
            entity.HasKey(e => e.UserNodeProgressId).HasName("user_node_progress_pkey");

            entity.ToTable("user_node_progress");

            entity.HasIndex(e => e.RoadmapEnrollmentId, "ix_user_node_progress_enrollment_id");

            entity.HasIndex(e => e.RoadmapNodeId, "ix_user_node_progress_node_id");

            entity.HasIndex(e => new { e.RoadmapEnrollmentId, e.RoadmapNodeId }, "uq_enrollment_node_progress").IsUnique();

            entity.Property(e => e.UserNodeProgressId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("user_node_progress_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.RoadmapEnrollmentId).HasColumnName("roadmap_enrollment_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");
            entity.Property(e => e.SkippedAt).HasColumnName("skipped_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.RoadmapEnrollment).WithMany(p => p.UserNodeProgresses)
                .HasForeignKey(d => d.RoadmapEnrollmentId)
                .HasConstraintName("fk_user_node_progress_enrollment");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.UserNodeProgresses)
                .HasForeignKey(d => d.RoadmapNodeId)
                .HasConstraintName("fk_user_node_progress_node");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_profile_pkey");

            entity.ToTable("user_profile");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Bio)
                .HasMaxLength(500)
                .HasColumnName("bio");
            entity.Property(e => e.CareerGoal)
                .HasMaxLength(150)
                .HasColumnName("career_goal");
            entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentRole)
                .HasMaxLength(100)
                .HasColumnName("current_role");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .HasColumnName("display_name");
            entity.Property(e => e.GithubUrl).HasColumnName("github_url");
            entity.Property(e => e.Headline)
                .HasMaxLength(150)
                .HasColumnName("headline");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.LinkedinUrl).HasColumnName("linkedin_url");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.PersonalWebsiteUrl).HasColumnName("personal_website_url");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(32)
                .HasColumnName("phone_number");
            entity.Property(e => e.PublicEmail)
                .HasMaxLength(254)
                .HasColumnName("public_email");
            entity.Property(e => e.ResumeUrl).HasColumnName("resume_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .HasConstraintName("fk_user_profile_user_id");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_pkey");

            entity.ToTable("user_role");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "uq_user_role").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_user_role_role_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_role_user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
