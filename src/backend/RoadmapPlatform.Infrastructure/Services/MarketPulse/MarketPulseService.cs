using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseService(
    ApplicationDbContext dbContext,
    IJobPortalScraper scraper,
    MarketPulseKeywordAnalyzer keywordAnalyzer,
    IOptions<MarketPulseSettings> options) : IMarketPulseService
{
    private const string AggregateSourceName = "all";
    private const string LifecycleActive = "active";
    private const string LifecycleStaleUnverified = "stale_unverified";
    private const string LifecycleExpired = "expired";
    private const string ObservationNew = "new";
    private const string ObservationSeen = "seen";
    private const string ObservationUpdated = "updated";

    public async Task<MarketPulseOverviewDto> GetOverviewAsync(
        int days,
        IReadOnlyCollection<string> skillSlugs,
        CancellationToken cancellationToken)
    {
        var normalizedDays = Math.Clamp(days, 7, 180);
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-(normalizedDays - 1)));
        var selectedSlugs = skillSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var snapshots = await dbContext.Set<SkillTrendSnapshot>()
            .AsNoTracking()
            .Where(x => x.SourceName == AggregateSourceName && x.SnapshotDate >= cutoffDate)
            .OrderBy(x => x.SnapshotDate)
            .ThenBy(x => x.SkillName)
            .ToListAsync(cancellationToken);

        var topSlugs = selectedSlugs.Count > 0
            ? selectedSlugs
            : snapshots
                .GroupBy(x => x.SkillSlug)
                .Select(g => g.OrderByDescending(x => x.SnapshotDate).First())
                .OrderByDescending(x => x.MentionCount)
                .Take(6)
                .Select(x => x.SkillSlug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visibleSlugs = selectedSlugs.Count > 0 ? selectedSlugs : topSlugs;
        var visibleSnapshots = snapshots
            .Where(x => visibleSlugs.Count == 0 || visibleSlugs.Contains(x.SkillSlug))
            .ToList();

        var latestDate = snapshots.Count > 0
            ? snapshots.Max(x => x.SnapshotDate).ToDateTime(TimeOnly.MinValue)
            : (DateTime?)null;

        var skills = snapshots
            .GroupBy(x => x.SkillSlug)
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.SnapshotDate).First();
                var previous = g
                    .Where(x => x.SnapshotDate < latest.SnapshotDate)
                    .OrderByDescending(x => x.SnapshotDate)
                    .FirstOrDefault();

                return new MarketSkillSummaryDto
                {
                    SkillName = latest.SkillName,
                    SkillSlug = latest.SkillSlug,
                    MentionCount = latest.MentionCount,
                    PostingCount = latest.PostingCount,
                    GrowthPercent = CalculateGrowth(latest.MentionCount, previous?.MentionCount ?? 0)
                };
            })
            .OrderByDescending(x => x.MentionCount)
            .ThenBy(x => x.SkillName)
            .ToList();

        var activeLookbackDays = Math.Clamp(options.Value.ActivePostingLookbackDays, 1, 90);
        var activeCutoff = DateTime.UtcNow.AddDays(-activeLookbackDays);

        var activePostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.LastSeenAt >= activeCutoff, cancellationToken);

        var stalePostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken);

        var expiredPostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken);

        var sourceCount = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.LastSeenAt >= activeCutoff)
            .Select(x => x.JobPortalSourceId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new MarketPulseOverviewDto
        {
            LastUpdatedAt = latestDate,
            TotalPostings = activePostings,
            ActivePostings = activePostings,
            StalePostings = stalePostings,
            ExpiredPostings = expiredPostings,
            SourceCount = sourceCount,
            Skills = skills,
            TrendPoints = visibleSnapshots.Select(x => new MarketTrendPointDto
            {
                Date = x.SnapshotDate.ToDateTime(TimeOnly.MinValue),
                SkillName = x.SkillName,
                SkillSlug = x.SkillSlug,
                MentionCount = x.MentionCount,
                PostingCount = x.PostingCount
            }).ToList()
        };
    }

    public async Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var snapshotDate = DateOnly.FromDateTime(now);
        var settings = options.Value;
        var rawPostings = await scraper.ScrapeAsync(cancellationToken);

        if (rawPostings.Count == 0)
        {
            return new MarketPulseRefreshResultDto
            {
                SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
                SourcesScraped = 0,
                PostingsScraped = 0,
                PostingsSaved = 0,
                NewPostings = 0,
                UpdatedPostings = 0,
                ActivePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.IsActive, cancellationToken),
                StalePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken),
                ExpiredPostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken),
                SkillSnapshotsSaved = 0
            };
        }

        var savedPostings = 0;
        var newPostings = 0;
        var updatedPostings = 0;
        var missingThreshold = Math.Max(1, settings.MissingScansBeforeStale);
        var minimumLifecyclePostings = Math.Max(1, settings.MinimumPostingsForLifecycleCheck);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var sourceGroup in rawPostings.GroupBy(x => x.SourceName))
        {
            var sourceSettings = settings.Sources.FirstOrDefault(x =>
                string.Equals(x.Name, sourceGroup.Key, StringComparison.OrdinalIgnoreCase));

            var source = await UpsertSourceAsync(sourceGroup.Key, sourceSettings, now, cancellationToken);
            var uniquePostings = sourceGroup
                .GroupBy(x => BuildExternalId(source.Name, x.Url), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            var externalIds = uniquePostings
                .Select(x => BuildExternalId(source.Name, x.Url))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingPostings = await dbContext.Set<JobPosting>()
                .Where(x => x.JobPortalSourceId == source.JobPortalSourceId && externalIds.Contains(x.ExternalId))
                .ToDictionaryAsync(x => x.ExternalId, cancellationToken);

            var observations = new List<JobPostingObservation>();

            foreach (var rawPosting in uniquePostings)
            {
                var externalId = BuildExternalId(source.Name, rawPosting.Url);
                var publishedAt = NormalizeUtc(rawPosting.PublishedAt);
                var expiresAt = NormalizeUtc(rawPosting.ExpiresAt);
                var contentHash = BuildContentHash(rawPosting, expiresAt);
                var isExpired = expiresAt.HasValue && DateOnly.FromDateTime(expiresAt.Value) < snapshotDate;

                if (existingPostings.TryGetValue(externalId, out var posting))
                {
                    var changed = !string.Equals(posting.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase);

                    posting.Title = TrimTo(rawPosting.Title, 250) ?? "Untitled IT job";
                    posting.CompanyName = TrimTo(rawPosting.CompanyName, 160);
                    posting.Location = TrimTo(rawPosting.Location, 160);
                    posting.Description = rawPosting.Description;
                    posting.PublishedAt = publishedAt;
                    posting.ExpiresAt = expiresAt;
                    posting.ContentHash = contentHash;
                    posting.IsActive = !isExpired;
                    posting.LifecycleStatus = isExpired ? LifecycleExpired : LifecycleActive;
                    posting.MissingScanCount = 0;
                    posting.SeenCount++;
                    posting.LastSeenAt = now;
                    posting.LastCheckedAt = now;
                    posting.LastChangedAt = changed ? now : posting.LastChangedAt;
                    posting.ClosedDetectedAt = isExpired && posting.ClosedDetectedAt is null
                        ? now
                        : posting.ClosedDetectedAt;
                    posting.ScrapedAt = now;
                    posting.UpdatedAt = now;

                    if (changed)
                    {
                        posting.UpdatedScanCount++;
                        updatedPostings++;
                    }

                    observations.Add(new JobPostingObservation(
                        posting,
                        changed ? ObservationUpdated : ObservationSeen,
                        contentHash));

                    continue;
                }

                var newPosting = new JobPosting
                {
                    JobPostingId = Guid.NewGuid(),
                    JobPortalSourceId = source.JobPortalSourceId,
                    ExternalId = externalId,
                    Title = TrimTo(rawPosting.Title, 250) ?? "Untitled IT job",
                    CompanyName = TrimTo(rawPosting.CompanyName, 160),
                    Location = TrimTo(rawPosting.Location, 160),
                    Url = rawPosting.Url,
                    Description = rawPosting.Description,
                    PublishedAt = publishedAt,
                    ExpiresAt = expiresAt,
                    ContentHash = contentHash,
                    LifecycleStatus = isExpired ? LifecycleExpired : LifecycleActive,
                    IsActive = !isExpired,
                    MissingScanCount = 0,
                    SeenCount = 1,
                    UpdatedScanCount = 0,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    LastCheckedAt = now,
                    LastChangedAt = now,
                    ClosedDetectedAt = isExpired ? now : null,
                    ScrapedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.Set<JobPosting>().Add(newPosting);
                observations.Add(new JobPostingObservation(newPosting, ObservationNew, contentHash));
                savedPostings++;
                newPostings++;
            }

            await UpsertDailyObservationsAsync(snapshotDate, source.Name, observations, now, cancellationToken);

            if (uniquePostings.Count >= minimumLifecyclePostings)
            {
                await MarkMissingPostingsAsync(source.JobPortalSourceId, externalIds, snapshotDate, now, missingThreshold, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var activeCutoff = now.AddDays(-Math.Clamp(settings.ActivePostingLookbackDays, 1, 90));
        var snapshotStartUtc = DateTime.SpecifyKind(snapshotDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var activePostingTexts = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.LastSeenAt >= activeCutoff &&
                (!x.ExpiresAt.HasValue || x.ExpiresAt.Value >= snapshotStartUtc))
            .Select(x => new { x.Title, x.Description })
            .ToListAsync(cancellationToken);

        var documents = activePostingTexts
            .Select(x => $"{x.Title} {x.Description}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var snapshotCount = 0;

        var definitions = keywordAnalyzer.BuildDefinitions(settings.TrackedKeywords);
        var frequencies = keywordAnalyzer.Analyze(documents, definitions);
        snapshotCount = await SaveSnapshotsAsync(snapshotDate, frequencies, cancellationToken);

        var result = new MarketPulseRefreshResultDto
        {
            SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
            SourcesScraped = rawPostings.Select(x => x.SourceName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            PostingsScraped = rawPostings.Count,
            PostingsSaved = savedPostings,
            NewPostings = newPostings,
            UpdatedPostings = updatedPostings,
            ActivePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.IsActive, cancellationToken),
            StalePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken),
            ExpiredPostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken),
            SkillSnapshotsSaved = snapshotCount
        };

        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private async Task<JobPortalSource> UpsertSourceAsync(
        string sourceName,
        MarketPulseSourceSettings? settings,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var source = await dbContext.Set<JobPortalSource>()
            .FirstOrDefaultAsync(x => x.Name == sourceName, cancellationToken);

        if (source != null)
        {
            source.BaseUrl = settings?.BaseUrl ?? source.BaseUrl;
            source.SearchUrlTemplate = settings?.SearchUrlTemplate ?? source.SearchUrlTemplate;
            source.IsEnabled = settings?.Enabled ?? source.IsEnabled;
            source.LastScrapedAt = now;
            source.UpdatedAt = now;
            return source;
        }

        source = new JobPortalSource
        {
            JobPortalSourceId = Guid.NewGuid(),
            Name = sourceName,
            BaseUrl = settings?.BaseUrl ?? string.Empty,
            SearchUrlTemplate = settings?.SearchUrlTemplate ?? string.Empty,
            IsEnabled = settings?.Enabled ?? true,
            LastScrapedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<JobPortalSource>().Add(source);

        return source;
    }

    private async Task UpsertDailyObservationsAsync(
        DateOnly snapshotDate,
        string sourceName,
        IReadOnlyCollection<JobPostingObservation> observations,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (observations.Count == 0)
        {
            return;
        }

        var postingIds = observations
            .Select(x => x.Posting.JobPostingId)
            .ToList();

        var existing = await dbContext.Set<JobPostingDailySnapshot>()
            .Where(x => x.SnapshotDate == snapshotDate && postingIds.Contains(x.JobPostingId))
            .ToDictionaryAsync(x => x.JobPostingId, cancellationToken);

        foreach (var observation in observations)
        {
            if (existing.TryGetValue(observation.Posting.JobPostingId, out var snapshot))
            {
                snapshot.SourceName = sourceName;
                snapshot.ObservationStatus = observation.Status;
                snapshot.ContentHash = observation.ContentHash;
                snapshot.ObservedAt = now;
                continue;
            }

            dbContext.Set<JobPostingDailySnapshot>().Add(new JobPostingDailySnapshot
            {
                JobPostingDailySnapshotId = Guid.NewGuid(),
                JobPostingId = observation.Posting.JobPostingId,
                SnapshotDate = snapshotDate,
                SourceName = sourceName,
                ObservationStatus = observation.Status,
                ContentHash = observation.ContentHash,
                ObservedAt = now,
                CreatedAt = now
            });
        }
    }

    private async Task MarkMissingPostingsAsync(
        Guid sourceId,
        IReadOnlySet<string> observedExternalIds,
        DateOnly snapshotDate,
        DateTime now,
        int missingThreshold,
        CancellationToken cancellationToken)
    {
        var missingPostings = await dbContext.Set<JobPosting>()
            .Where(x =>
                x.JobPortalSourceId == sourceId &&
                x.LifecycleStatus != LifecycleExpired &&
                !observedExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken);

        foreach (var posting in missingPostings)
        {
            if (DateOnly.FromDateTime(posting.LastCheckedAt) < snapshotDate)
            {
                posting.MissingScanCount++;
            }

            posting.LastCheckedAt = now;

            if (posting.ExpiresAt.HasValue && DateOnly.FromDateTime(posting.ExpiresAt.Value) < snapshotDate)
            {
                posting.IsActive = false;
                posting.LifecycleStatus = LifecycleExpired;
                posting.ClosedDetectedAt ??= now;
            }
            else if (posting.MissingScanCount >= missingThreshold)
            {
                posting.IsActive = false;
                posting.LifecycleStatus = LifecycleStaleUnverified;
            }

            posting.UpdatedAt = now;
        }
    }

    private async Task<int> SaveSnapshotsAsync(
        DateOnly snapshotDate,
        IReadOnlyCollection<KeywordFrequency> frequencies,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<SkillTrendSnapshot>()
            .Where(x => x.SnapshotDate == snapshotDate && x.SourceName == AggregateSourceName)
            .ToDictionaryAsync(x => x.SkillSlug, cancellationToken);

        var count = 0;
        var currentSlugs = frequencies
            .Select(x => x.SkillSlug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var staleSnapshots = existing
            .Where(x => !currentSlugs.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        if (staleSnapshots.Count > 0)
        {
            dbContext.Set<SkillTrendSnapshot>().RemoveRange(staleSnapshots);
        }

        foreach (var frequency in frequencies)
        {
            if (existing.TryGetValue(frequency.SkillSlug, out var snapshot))
            {
                snapshot.SkillName = frequency.SkillName;
                snapshot.MentionCount = frequency.MentionCount;
                snapshot.PostingCount = frequency.PostingCount;
                count++;
                continue;
            }

            dbContext.Set<SkillTrendSnapshot>().Add(new SkillTrendSnapshot
            {
                SnapshotDate = snapshotDate,
                SkillName = frequency.SkillName,
                SkillSlug = frequency.SkillSlug,
                SourceName = AggregateSourceName,
                MentionCount = frequency.MentionCount,
                PostingCount = frequency.PostingCount,
                CreatedAt = DateTime.UtcNow
            });
            count++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }

    private static decimal CalculateGrowth(int current, int previous)
    {
        if (previous <= 0)
        {
            return current > 0 ? 100 : 0;
        }

        return Math.Round(((decimal)(current - previous) / previous) * 100, 1);
    }

    private static string BuildExternalId(string sourceName, string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{sourceName}:{url}".ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildContentHash(ScrapedJobPosting posting, DateTime? expiresAt)
    {
        var normalized = string.Join('\n',
            NormalizeForHash(posting.Title),
            NormalizeForHash(posting.CompanyName),
            NormalizeForHash(posting.Location),
            NormalizeForHash(posting.Description),
            expiresAt?.ToString("O") ?? string.Empty);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static string NormalizeForHash(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

internal sealed record JobPostingObservation(JobPosting Posting, string Status, string ContentHash);
