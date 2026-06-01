using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatbotMessage> ChatbotMessages { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<MyResource> MyResources { get; set; }

    public virtual DbSet<NodeSkill> NodeSkills { get; set; }

    public virtual DbSet<OtherResource> OtherResources { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionRole> PermissionRoles { get; set; }

    public virtual DbSet<RepoInsight> RepoInsights { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceChunk> ResourceChunks { get; set; }

    public virtual DbSet<Roadmap> Roadmaps { get; set; }

    public virtual DbSet<RoadmapEdge> RoadmapEdges { get; set; }

    public virtual DbSet<RoadmapNode> RoadmapNodes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Specialty> Specialties { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivityStat> UserActivityStats { get; set; }

    public virtual DbSet<UserAuthProvider> UserAuthProviders { get; set; }

    public virtual DbSet<UserInsight> UserInsights { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRoadmapStatus> UserRoadmapStatuses { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserSkillProgress> UserSkillProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("vector");

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
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.Purpose)
                .HasMaxLength(50)
                .HasColumnName("purpose");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
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

        modelBuilder.Entity<NodeSkill>(entity =>
        {
            entity.HasKey(e => e.NodeSkillId).HasName("node_skill_pkey");

            entity.ToTable("node_skill");

            entity.HasIndex(e => new { e.RoadmapNodeId, e.SkillId }, "uq_node_skill").IsUnique();

            entity.Property(e => e.NodeSkillId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("node_skill_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.NodeSkills)
                .HasForeignKey(d => d.RoadmapNodeId)
                .HasConstraintName("fk_node_skill_roadmap_node_id");

            entity.HasOne(d => d.Skill).WithMany(p => p.NodeSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_node_skill_skill_id");
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

        modelBuilder.Entity<RepoInsight>(entity =>
        {
            entity.HasKey(e => e.InsightId).HasName("repo_insight_pkey");

            entity.ToTable("repo_insight");

            entity.Property(e => e.InsightId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("insight_id");
            entity.Property(e => e.AnalyzedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("analyzed_at");
            entity.Property(e => e.DetectedSkills)
                .HasColumnType("jsonb")
                .HasColumnName("detected_skills");
            entity.Property(e => e.ProjectType)
                .HasMaxLength(100)
                .HasColumnName("project_type");
            entity.Property(e => e.RepositoryId).HasColumnName("repository_id");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.TechStack)
                .HasColumnType("jsonb")
                .HasColumnName("tech_stack");

            entity.HasOne(d => d.Repository).WithMany(p => p.RepoInsights)
                .HasForeignKey(d => d.RepositoryId)
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

            entity.Property(e => e.RoadmapId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("roadmap_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RoadmapName)
                .HasMaxLength(100)
                .HasColumnName("roadmap_name");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version");

            entity.HasOne(d => d.Specialty).WithMany(p => p.Roadmaps)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_roadmap_specialty_id");
        });

        modelBuilder.Entity<RoadmapEdge>(entity =>
        {
            entity.HasKey(e => e.EdgeId).HasName("roadmap_edge_pkey");

            entity.ToTable("roadmap_edge");

            entity.HasIndex(e => new { e.RoadmapId, e.AncestorNodeId, e.DescendantNodeId }, "uq_roadmap_edge").IsUnique();

            entity.Property(e => e.EdgeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("edge_id");
            entity.Property(e => e.AncestorNodeId).HasColumnName("ancestor_node_id");
            entity.Property(e => e.DescendantNodeId).HasColumnName("descendant_node_id");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");

            entity.HasOne(d => d.AncestorNode).WithMany(p => p.RoadmapEdgeAncestorNodes)
                .HasForeignKey(d => d.AncestorNodeId)
                .HasConstraintName("fk_roadmap_edge_ancestor_node_id");

            entity.HasOne(d => d.DescendantNode).WithMany(p => p.RoadmapEdgeDescendantNodes)
                .HasForeignKey(d => d.DescendantNodeId)
                .HasConstraintName("fk_roadmap_edge_descendant_node_id");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.RoadmapEdges)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("fk_roadmap_edge_roadmap_id");
        });

        modelBuilder.Entity<RoadmapNode>(entity =>
        {
            entity.HasKey(e => e.NodeId).HasName("roadmap_node_pkey");

            entity.ToTable("roadmap_node");

            entity.Property(e => e.NodeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("node_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsMandatory)
                .HasDefaultValue(true)
                .HasColumnName("is_mandatory");
            entity.Property(e => e.PositionX).HasColumnName("position_x");
            entity.Property(e => e.PositionY).HasColumnName("position_y");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.RoadmapNodes)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("fk_roadmap_node_roadmap_id");
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

            entity.HasIndex(e => e.SkillName, "skill_skill_name_key").IsUnique();

            entity.Property(e => e.SkillId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.SpecialtyId).HasName("specialty_pkey");

            entity.ToTable("specialty");

            entity.HasIndex(e => e.SpecialtyName, "specialty_specialty_name_key").IsUnique();

            entity.Property(e => e.SpecialtyId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("specialty_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SpecialtyName)
                .HasMaxLength(100)
                .HasColumnName("specialty_name");
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

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_auth_provider_pkey");

            entity.ToTable("user_auth_provider");

            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }, "uq_provider_identity").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.Provider }, "uq_user_provider").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
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

        modelBuilder.Entity<UserRoadmapStatus>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("user_roadmap_status_pkey");

            entity.ToTable("user_roadmap_status");

            entity.HasIndex(e => new { e.UserId, e.RoadmapId }, "uq_user_roadmap_status").IsUnique();

            entity.Property(e => e.EnrollmentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("enrollment_id");
            entity.Property(e => e.LastTime)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_time");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Roadmap).WithMany(p => p.UserRoadmapStatuses)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("fk_user_roadmap_status_roadmap_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoadmapStatuses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_roadmap_status_user_id");
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

        modelBuilder.Entity<UserSkillProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("user_skill_progress_pkey");

            entity.ToTable("user_skill_progress");

            entity.HasIndex(e => new { e.UserId, e.SkillId }, "uq_user_skill_progress").IsUnique();

            entity.Property(e => e.ProgressId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("progress_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.UnlockMethod)
                .HasColumnType("jsonb")
                .HasColumnName("unlock_method");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Skill).WithMany(p => p.UserSkillProgresses)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("fk_user_skill_progress_skill_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSkillProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_skill_progress_user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
