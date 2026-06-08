using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public virtual DbSet<JobPortalSource> JobPortalSources => Set<JobPortalSource>();

    public virtual DbSet<JobPosting> JobPostings => Set<JobPosting>();

    public virtual DbSet<JobPostingDailySnapshot> JobPostingDailySnapshots => Set<JobPostingDailySnapshot>();

    public virtual DbSet<SkillTrendSnapshot> SkillTrendSnapshots => Set<SkillTrendSnapshot>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobPortalSource>(entity =>
        {
            entity.HasKey(e => e.JobPortalSourceId).HasName("job_portal_source_pkey");

            entity.ToTable("job_portal_source");

            entity.HasIndex(e => e.Name, "uq_job_portal_source_name").IsUnique();

            entity.Property(e => e.JobPortalSourceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_portal_source_id");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");
            entity.Property(e => e.BaseUrl).HasColumnName("base_url");
            entity.Property(e => e.SearchUrlTemplate).HasColumnName("search_url_template");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            entity.Property(e => e.LastScrapedAt).HasColumnName("last_scraped_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.JobPostingId).HasName("job_posting_pkey");

            entity.ToTable("job_posting");

            entity.HasIndex(e => new { e.JobPortalSourceId, e.ExternalId }, "uq_job_posting_source_external")
                .IsUnique();
            entity.HasIndex(e => e.ScrapedAt, "ix_job_posting_scraped_at");
            entity.HasIndex(e => e.Title, "ix_job_posting_title");
            entity.HasIndex(e => new { e.IsActive, e.LastSeenAt }, "ix_job_posting_active_last_seen");
            entity.HasIndex(e => e.LifecycleStatus, "ix_job_posting_lifecycle_status");

            entity.Property(e => e.JobPostingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_posting_id");
            entity.Property(e => e.JobPortalSourceId).HasColumnName("job_portal_source_id");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(120)
                .HasColumnName("external_id");
            entity.Property(e => e.Title)
                .HasMaxLength(250)
                .HasColumnName("title");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(160)
                .HasColumnName("company_name");
            entity.Property(e => e.Location)
                .HasMaxLength(160)
                .HasColumnName("location");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .HasColumnName("content_hash");
            entity.Property(e => e.LifecycleStatus)
                .HasMaxLength(32)
                .HasDefaultValue("active")
                .HasColumnName("lifecycle_status");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MissingScanCount).HasColumnName("missing_scan_count");
            entity.Property(e => e.SeenCount)
                .HasDefaultValue(1)
                .HasColumnName("seen_count");
            entity.Property(e => e.UpdatedScanCount).HasColumnName("updated_scan_count");
            entity.Property(e => e.FirstSeenAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("first_seen_at");
            entity.Property(e => e.LastSeenAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_seen_at");
            entity.Property(e => e.LastCheckedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_checked_at");
            entity.Property(e => e.LastChangedAt).HasColumnName("last_changed_at");
            entity.Property(e => e.ClosedDetectedAt).HasColumnName("closed_detected_at");
            entity.Property(e => e.ScrapedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("scraped_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.JobPortalSource).WithMany(p => p.JobPostings)
                .HasForeignKey(d => d.JobPortalSourceId)
                .HasConstraintName("fk_job_posting_source");
        });

        modelBuilder.Entity<JobPostingDailySnapshot>(entity =>
        {
            entity.HasKey(e => e.JobPostingDailySnapshotId).HasName("job_posting_daily_snapshot_pkey");

            entity.ToTable("job_posting_daily_snapshot");

            entity.HasIndex(e => new { e.JobPostingId, e.SnapshotDate }, "uq_job_posting_daily_snapshot")
                .IsUnique();
            entity.HasIndex(e => e.SnapshotDate, "ix_job_posting_daily_snapshot_date");

            entity.Property(e => e.JobPostingDailySnapshotId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("job_posting_daily_snapshot_id");
            entity.Property(e => e.JobPostingId).HasColumnName("job_posting_id");
            entity.Property(e => e.SnapshotDate)
                .HasColumnType("date")
                .HasColumnName("snapshot_date");
            entity.Property(e => e.SourceName)
                .HasMaxLength(80)
                .HasColumnName("source_name");
            entity.Property(e => e.ObservationStatus)
                .HasMaxLength(32)
                .HasColumnName("observation_status");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .HasColumnName("content_hash");
            entity.Property(e => e.ObservedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("observed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.JobPosting).WithMany()
                .HasForeignKey(d => d.JobPostingId)
                .HasConstraintName("fk_job_posting_daily_snapshot_posting");
        });
        
        modelBuilder.Entity<SkillTrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.SkillTrendSnapshotId).HasName("skill_trend_snapshot_pkey");

            entity.ToTable("skill_trend_snapshot");

            entity.HasIndex(e => new { e.SkillSlug, e.SnapshotDate, e.SourceName }, "uq_skill_trend_snapshot")
                .IsUnique();
            entity.HasIndex(e => e.SnapshotDate, "ix_skill_trend_snapshot_date");

            entity.Property(e => e.SkillTrendSnapshotId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("skill_trend_snapshot_id");
            entity.Property(e => e.SnapshotDate)
                .HasColumnType("date")
                .HasColumnName("snapshot_date");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");
            entity.Property(e => e.SkillSlug)
                .HasMaxLength(120)
                .HasColumnName("skill_slug");
            entity.Property(e => e.SourceName)
                .HasMaxLength(80)
                .HasDefaultValue("all")
                .HasColumnName("source_name");
            entity.Property(e => e.MentionCount).HasColumnName("mention_count");
            entity.Property(e => e.PostingCount).HasColumnName("posting_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
        });
    }
}
