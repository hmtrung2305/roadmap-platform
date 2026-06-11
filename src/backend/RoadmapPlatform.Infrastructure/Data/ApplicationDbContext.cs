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

    public virtual DbSet<CareerRole> CareerRoles { get; set; }

    public virtual DbSet<ChatbotMessage> ChatbotMessages { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<JobPortalSource> JobPortalSources { get; set; }

    public virtual DbSet<JobPosting> JobPostings { get; set; }

    public virtual DbSet<JobPostingDailySnapshot> JobPostingDailySnapshots { get; set; }

    public virtual DbSet<LearningResource> LearningResources { get; set; }

    public virtual DbSet<LearningResourceSkill> LearningResourceSkills { get; set; }

    public virtual DbSet<MyResource> MyResources { get; set; }

    public virtual DbSet<OtherResource> OtherResources { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<PendingLocalRegistration> PendingLocalRegistrations { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionRole> PermissionRoles { get; set; }

    public virtual DbSet<ProgressEvent> ProgressEvents { get; set; }

    public virtual DbSet<RepoInsight> RepoInsights { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceChunk> ResourceChunks { get; set; }

    public virtual DbSet<Roadmap> Roadmaps { get; set; }

    public virtual DbSet<RoadmapEdge> RoadmapEdges { get; set; }

    public virtual DbSet<RoadmapEnrollment> RoadmapEnrollments { get; set; }

    public virtual DbSet<RoadmapNode> RoadmapNodes { get; set; }

    public virtual DbSet<RoadmapNodeResource> RoadmapNodeResources { get; set; }

    public virtual DbSet<RoadmapNodeSkill> RoadmapNodeSkills { get; set; }

    public virtual DbSet<RoadmapVersion> RoadmapVersions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SkillTrendSnapshot> SkillTrendSnapshots { get; set; }

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

        modelBuilder.Entity<ChatbotMessage>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("chatbot_message_pkey");

            entity.ToTable("chatbot_message");

            entity.Property(e => e.RequestId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("request_id");
            entity.Property(e => e.ContentMessage).HasColumnName("content_message");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ChatbotMessages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("fk_chatbot_message_conversation_id");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("conversation_pkey");

            entity.ToTable("conversation");

            entity.Property(e => e.ConversationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Resource).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("fk_conversation_resource_id");

            entity.HasOne(d => d.User).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_conversation_user_id");
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

        modelBuilder.Entity<JobPortalSource>(entity =>
        {
            entity.HasKey(e => e.JobPortalSourceId).HasName("job_portal_source_pkey");

            entity.ToTable("job_portal_source");

            entity.HasIndex(e => e.Name, "uq_job_portal_source_name").IsUnique();

            entity.Property(e => e.JobPortalSourceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_portal_source_id");
            entity.Property(e => e.BaseUrl).HasColumnName("base_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            entity.Property(e => e.LastScrapedAt).HasColumnName("last_scraped_at");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");
            entity.Property(e => e.SearchUrlTemplate).HasColumnName("search_url_template");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.JobPostingId).HasName("job_posting_pkey");

            entity.ToTable("job_posting");

            entity.HasIndex(e => new { e.IsActive, e.LastSeenAt }, "ix_job_posting_active_last_seen");

            entity.HasIndex(e => e.LifecycleStatus, "ix_job_posting_lifecycle_status");

            entity.HasIndex(e => e.ScrapedAt, "ix_job_posting_scraped_at");

            entity.HasIndex(e => e.Title, "ix_job_posting_title");

            entity.HasIndex(e => new { e.JobPortalSourceId, e.ExternalId }, "uq_job_posting_source_external").IsUnique();

            entity.Property(e => e.JobPostingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_posting_id");
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
            entity.Property(e => e.JobPortalSourceId).HasColumnName("job_portal_source_id");
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
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ScrapedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("scraped_at");
            entity.Property(e => e.SeenCount)
                .HasDefaultValue(1)
                .HasColumnName("seen_count");
            entity.Property(e => e.Title)
                .HasMaxLength(250)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedScanCount).HasColumnName("updated_scan_count");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.JobPortalSource).WithMany(p => p.JobPostings)
                .HasForeignKey(d => d.JobPortalSourceId)
                .HasConstraintName("fk_job_posting_source");
        });

        modelBuilder.Entity<JobPostingDailySnapshot>(entity =>
        {
            entity.HasKey(e => e.JobPostingDailySnapshotId).HasName("job_posting_daily_snapshot_pkey");

            entity.ToTable("job_posting_daily_snapshot");

            entity.HasIndex(e => e.SnapshotDate, "ix_job_posting_daily_snapshot_date");

            entity.HasIndex(e => new { e.JobPostingId, e.SnapshotDate }, "uq_job_posting_daily_snapshot").IsUnique();

            entity.Property(e => e.JobPostingDailySnapshotId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_posting_daily_snapshot_id");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .HasColumnName("content_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.JobPostingId).HasColumnName("job_posting_id");
            entity.Property(e => e.ObservationStatus)
                .HasMaxLength(32)
                .HasColumnName("observation_status");
            entity.Property(e => e.ObservedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("observed_at");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshot_date");
            entity.Property(e => e.SourceName)
                .HasMaxLength(80)
                .HasColumnName("source_name");

            entity.HasOne(d => d.JobPosting).WithMany(p => p.JobPostingDailySnapshots)
                .HasForeignKey(d => d.JobPostingId)
                .HasConstraintName("fk_job_posting_daily_snapshot_posting");
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

        modelBuilder.Entity<MyResource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("my_resource_pkey");

            entity.ToTable("my_resource");

            entity.Property(e => e.ResourceId)
                .ValueGeneratedNever()
                .HasColumnName("resource_id");

            entity.HasOne(d => d.Resource).WithOne(p => p.MyResource)
                .HasForeignKey<MyResource>(d => d.ResourceId)
                .HasConstraintName("fk_my_resource_id");
        });

        modelBuilder.Entity<OtherResource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("other_resource_pkey");

            entity.ToTable("other_resource");

            entity.Property(e => e.ResourceId)
                .ValueGeneratedNever()
                .HasColumnName("resource_id");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .HasColumnName("provider");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(100)
                .HasColumnName("resource_type");

            entity.HasOne(d => d.Resource).WithOne(p => p.OtherResource)
                .HasForeignKey<OtherResource>(d => d.ResourceId)
                .HasConstraintName("fk_other_resource_id");
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
                .HasMaxLength(50)
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

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("resource_pkey");

            entity.ToTable("resource");

            entity.Property(e => e.ResourceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("resource_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");
            entity.Property(e => e.Url)
                .HasMaxLength(100)
                .HasColumnName("url");

            entity.HasOne(d => d.Skill).WithMany(p => p.Resources)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_skill_id");
        });

        modelBuilder.Entity<ResourceChunk>(entity =>
        {
            entity.HasKey(e => e.ChunkId).HasName("resource_chunk_pkey");

            entity.ToTable("resource_chunk");

            entity.Property(e => e.ChunkId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("chunk_id");
            entity.Property(e => e.ChunkContent).HasColumnName("chunk_content");
            entity.Property(e => e.Embedding)
                .HasMaxLength(3072)
                .HasColumnName("embedding");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");

            entity.HasOne(d => d.Resource).WithMany(p => p.ResourceChunks)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("fk_chunk_resource");
        });

        modelBuilder.Entity<Roadmap>(entity =>
        {
            entity.HasKey(e => e.RoadmapId).HasName("roadmap_pkey");

            entity.ToTable("roadmap");

            entity.HasIndex(e => e.CareerRoleId, "ix_roadmap_career_role_id");

            entity.HasIndex(e => e.OwnerUserId, "ix_roadmap_owner_user_id");

            entity.HasIndex(e => e.SourceType, "ix_roadmap_source_type");

            entity.HasIndex(e => new { e.RoadmapType, e.Visibility }, "ix_roadmap_type_visibility");

            entity.Property(e => e.RoadmapId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_id");
            entity.Property(e => e.CareerRoleId).HasColumnName("career_role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.RoadmapType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'template'::character varying")
                .HasColumnName("roadmap_type");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'static'::character varying")
                .HasColumnName("source_type");
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

            entity.HasIndex(e => new { e.RoadmapVersionId, e.LayoutGroup }, "ix_roadmap_node_layout_group");

            entity.HasIndex(e => new { e.RoadmapVersionId, e.LayoutRank, e.LayoutOrder }, "ix_roadmap_node_layout_rank_order");

            entity.HasIndex(e => e.LayoutRole, "ix_roadmap_node_layout_role");

            entity.HasIndex(e => e.ParentNodeId, "ix_roadmap_node_parent_id");

            entity.HasIndex(e => new { e.RoadmapVersionId, e.PositionX, e.PositionY }, "ix_roadmap_node_position");

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
            entity.Property(e => e.LayoutGroup)
                .HasMaxLength(80)
                .HasColumnName("layout_group");
            entity.Property(e => e.LayoutOrder).HasColumnName("layout_order");
            entity.Property(e => e.LayoutRank).HasColumnName("layout_rank");
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
            entity.Property(e => e.PositionX)
                .HasPrecision(10, 2)
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasPrecision(10, 2)
                .HasColumnName("position_y");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Reason).HasColumnName("reason");
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
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.LearningResourceId).HasColumnName("learning_resource_id");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");

            entity.HasOne(d => d.LearningResource).WithMany(p => p.RoadmapNodeResources)
                .HasForeignKey(d => d.LearningResourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_roadmap_node_resource_resource");
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

            entity.HasOne(d => d.Skill).WithMany(p => p.RoadmapNodeSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_roadmap_node_skill_skill");
        });

        modelBuilder.Entity<RoadmapVersion>(entity =>
        {
            entity.HasKey(e => e.RoadmapVersionId).HasName("roadmap_version_pkey");

            entity.ToTable("roadmap_version");

            entity.HasIndex(e => e.GeneratedByUserId, "ix_roadmap_version_generated_by_user_id");

            entity.HasIndex(e => e.GenerationStatus, "ix_roadmap_version_generation_status");

            entity.HasIndex(e => e.RoadmapId, "ix_roadmap_version_roadmap_id");

            entity.HasIndex(e => e.Status, "ix_roadmap_version_status");

            entity.HasIndex(e => new { e.RoadmapId, e.VersionNumber }, "uq_roadmap_version_number").IsUnique();

            entity.Property(e => e.RoadmapVersionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_version_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedTotalHours).HasColumnName("estimated_total_hours");
            entity.Property(e => e.GeneratedByUserId).HasColumnName("generated_by_user_id");
            entity.Property(e => e.GenerationContext)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("generation_context");
            entity.Property(e => e.GenerationError).HasColumnName("generation_error");
            entity.Property(e => e.GenerationModel)
                .HasMaxLength(100)
                .HasColumnName("generation_model");
            entity.Property(e => e.GenerationPrompt).HasColumnName("generation_prompt");
            entity.Property(e => e.GenerationStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'none'::character varying")
                .HasColumnName("generation_status");
            entity.Property(e => e.LayoutAlgorithm)
                .HasMaxLength(50)
                .HasColumnName("layout_algorithm");
            entity.Property(e => e.LayoutDirection)
                .HasMaxLength(20)
                .HasDefaultValueSql("'TB'::character varying")
                .HasColumnName("layout_direction");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'draft'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");

            entity.HasOne(d => d.GeneratedByUser).WithMany(p => p.RoadmapVersions)
                .HasForeignKey(d => d.GeneratedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_roadmap_version_generated_by_user");
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
                .HasMaxLength(15)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("skill_pkey");

            entity.ToTable("skill");

            entity.HasIndex(e => e.Category, "ix_skill_category");

            entity.HasIndex(e => e.Slug, "ix_skill_slug");

            entity.HasIndex(e => e.Name, "skill_skill_name_key").IsUnique();

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

        modelBuilder.Entity<SkillTrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.SkillTrendSnapshotId).HasName("skill_trend_snapshot_pkey");

            entity.ToTable("skill_trend_snapshot");

            entity.HasIndex(e => e.SnapshotDate, "ix_skill_trend_snapshot_date");

            entity.HasIndex(e => new { e.SkillSlug, e.SnapshotDate, e.SourceName }, "uq_skill_trend_snapshot").IsUnique();

            entity.Property(e => e.SkillTrendSnapshotId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_trend_snapshot_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MentionCount).HasColumnName("mention_count");
            entity.Property(e => e.PostingCount).HasColumnName("posting_count");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");
            entity.Property(e => e.SkillSlug)
                .HasMaxLength(120)
                .HasColumnName("skill_slug");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshot_date");
            entity.Property(e => e.SourceName)
                .HasMaxLength(80)
                .HasDefaultValueSql("'all'::character varying")
                .HasColumnName("source_name");
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
            entity.Property(e => e.EvidenceUrl).HasColumnName("evidence_url");
            entity.Property(e => e.LearnerNote).HasColumnName("learner_note");
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
