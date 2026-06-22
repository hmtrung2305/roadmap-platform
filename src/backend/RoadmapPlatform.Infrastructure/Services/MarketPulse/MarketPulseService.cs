using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Application.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseService(
    ApplicationDbContext dbContext,
    IJobPortalScraper scraper,
    JobMarketOverviewBuilder overviewBuilder,
    JobMarketKeywordAnalyzer keywordAnalyzer,
    IMemoryCache cache,
    IOptions<MarketPulseSettings> options) : IMarketPulseService
{
    private const string AggregateSourceName = "all";
    private const string OverviewCacheVersionKey = "market-pulse:overview:version";
    private const string LifecycleActive = "active";
    private const string LifecycleStaleUnverified = "stale_unverified";
    private const string LifecycleExpired = "expired";
    private const string ObservationNew = "new";
    private const string ObservationSeen = "seen";
    private const string ObservationUpdated = "updated";

    public async Task<MarketPulseOverviewDto> GetOverviewAsync(
        MarketPulseOverviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = NormalizeQuery(query);
        var cacheSeconds = Math.Clamp(options.Value.OverviewCacheSeconds, 0, 300);
        if (cacheSeconds <= 0)
        {
            return await BuildOverviewFromDatabaseAsync(normalizedQuery, cancellationToken);
        }

        var version = GetOverviewCacheVersion();
        var cacheKey = BuildOverviewCacheKey(normalizedQuery, version);

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheSeconds);
                return await BuildOverviewFromDatabaseAsync(normalizedQuery, cancellationToken);
            }) ?? new MarketPulseOverviewDto();
    }

    private async Task<MarketPulseOverviewDto> BuildOverviewFromDatabaseAsync(
        MarketPulseOverviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var todayStart = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);
        var activeLookbackDays = Math.Clamp(settings.ActivePostingLookbackDays, 1, 90);
        var activeCutoff = now.AddDays(-activeLookbackDays);
        var maxPostings = Math.Clamp(Math.Max(settings.MaxPostingsPerSource, 500), 100, 5_000);
        var sourceFilter = NormalizeFilter(query.Source);
        var categoryFilter = NormalizeFilter(query.Category);
        var locationFilter = NormalizeFilter(query.Location);

        var activePostingCount = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.LastSeenAt >= activeCutoff)
            .Where(x => sourceFilter == null || x.JobPortalSource.Name == sourceFilter)
            .Where(x => categoryFilter == null || x.Category == categoryFilter)
            .Where(x => locationFilter == null || x.Location == locationFilter)
            .CountAsync(cancellationToken);

        var todayPostingCount = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x =>
                x.IsActive &&
                (sourceFilter == null || x.JobPortalSource.Name == sourceFilter) &&
                (categoryFilter == null || x.Category == categoryFilter) &&
                (locationFilter == null || x.Location == locationFilter) &&
                x.PublishedAt.HasValue &&
                x.PublishedAt.Value >= todayStart &&
                x.PublishedAt.Value < tomorrowStart,
                cancellationToken);

        var stalePostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken);

        var expiredPostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken);

        var postings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Include(x => x.JobPortalSource)
            .Where(x => x.IsActive && x.LastSeenAt >= activeCutoff)
            .Where(x => sourceFilter == null || x.JobPortalSource.Name == sourceFilter)
            .Where(x => categoryFilter == null || x.Category == categoryFilter)
            .Where(x => locationFilter == null || x.Location == locationFilter)
            .OrderByDescending(x => x.PublishedAt ?? x.LastSeenAt)
            .ThenByDescending(x => x.UpdatedAt)
            .Take(maxPostings)
            .ToListAsync(cancellationToken);

        var activeJobs = postings
            .Select(ToJobMarketPosting)
            .Where(x => MatchesMemoryFilters(x, query))
            .ToList();
        var todayJobs = activeJobs
            .Where(x => x.PostedOn == today)
            .ToList();
        var useMemoryTotals = HasMemoryFilters(query);

        var overview = overviewBuilder.Build(
            new JobMarketSnapshot
            {
                ActiveTotal = useMemoryTotals ? activeJobs.Count : activePostingCount,
                TodayTotal = useMemoryTotals ? todayJobs.Count : todayPostingCount,
                ActiveJobs = activeJobs,
                TodayJobs = todayJobs
            },
            new JobMarketOverviewOptions
            {
                Days = query.Days,
                SelectedSkillSlugs = query.SkillSlugs,
                TrackedKeywordSpecs = settings.TrackedKeywords,
                ReferenceDate = today
            });

        overview.StalePostings = stalePostings;
        overview.ExpiredPostings = expiredPostings;
        return overview;
    }

    private static JobMarketPosting ToJobMarketPosting(JobPosting posting)
    {
        return new JobMarketPosting
        {
            Id = posting.ExternalId,
            SourceJobId = posting.SourceJobId,
            Source = posting.JobPortalSource?.Name,
            Title = posting.Title,
            Company = posting.CompanyName,
            Category = posting.Category,
            Location = posting.Location,
            Salary = posting.Salary,
            Experience = posting.Experience,
            PostedOn = posting.PublishedAt.HasValue ? DateOnly.FromDateTime(posting.PublishedAt.Value) : null,
            PostedOnText = posting.PostDateText,
            UpdatedAt = posting.SourceUpdatedAt ?? posting.UpdatedAt,
            Url = posting.Url,
            IsActive = posting.IsActive,
            Requirements = DeserializeStringList(posting.Requirements),
            Specialties = DeserializeStringList(posting.Specialties),
            Benefits = DeserializeStringList(posting.Benefits)
        };
    }

    private static MarketPulseOverviewQueryDto NormalizeQuery(MarketPulseOverviewQueryDto query)
    {
        var salaryMin = query.SalaryMinMonthlyVnd;
        var salaryMax = query.SalaryMaxMonthlyVnd;
        if (salaryMin.HasValue && salaryMax.HasValue && salaryMin > salaryMax)
        {
            (salaryMin, salaryMax) = (salaryMax, salaryMin);
        }

        return new MarketPulseOverviewQueryDto
        {
            Days = Math.Clamp(query.Days, 7, 180),
            SkillSlugs = query.SkillSlugs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList(),
            Category = NormalizeFilter(query.Category),
            Location = NormalizeFilter(query.Location),
            Experience = NormalizeFilter(query.Experience),
            Source = NormalizeFilter(query.Source),
            SalaryMinMonthlyVnd = salaryMin,
            SalaryMaxMonthlyVnd = salaryMax
        };
    }

    private string GetOverviewCacheVersion()
    {
        return cache.GetOrCreate(OverviewCacheVersionKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return Guid.NewGuid().ToString("N");
        }) ?? "initial";
    }

    private void InvalidateOverviewCache()
    {
        cache.Set(
            OverviewCacheVersionKey,
            Guid.NewGuid().ToString("N"),
            new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
    }

    private static string BuildOverviewCacheKey(MarketPulseOverviewQueryDto query, string version)
    {
        return string.Join(
            '|',
            "market-pulse:overview",
            version,
            query.Days.ToString(CultureInfo.InvariantCulture),
            string.Join(',', query.SkillSlugs.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
            query.Category ?? string.Empty,
            query.Location ?? string.Empty,
            query.Experience ?? string.Empty,
            query.Source ?? string.Empty,
            query.SalaryMinMonthlyVnd?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            query.SalaryMaxMonthlyVnd?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool MatchesMemoryFilters(JobMarketPosting posting, MarketPulseOverviewQueryDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Experience) &&
            !ContainsNormalized(posting.Experience, query.Experience) &&
            !ContainsNormalized(posting.Title, query.Experience))
        {
            return false;
        }

        if (query.SalaryMinMonthlyVnd.HasValue || query.SalaryMaxMonthlyVnd.HasValue)
        {
            var salaryRange = TryParseMonthlySalary(posting.Salary);
            if (salaryRange is null)
            {
                return false;
            }

            if (query.SalaryMinMonthlyVnd.HasValue &&
                salaryRange.MaxMonthlyVnd.GetValueOrDefault(salaryRange.MinMonthlyVnd ?? 0) < query.SalaryMinMonthlyVnd.Value)
            {
                return false;
            }

            if (query.SalaryMaxMonthlyVnd.HasValue &&
                salaryRange.MinMonthlyVnd.GetValueOrDefault(salaryRange.MaxMonthlyVnd ?? decimal.MaxValue) > query.SalaryMaxMonthlyVnd.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasMemoryFilters(MarketPulseOverviewQueryDto query)
    {
        return !string.IsNullOrWhiteSpace(query.Experience) ||
            query.SalaryMinMonthlyVnd.HasValue ||
            query.SalaryMaxMonthlyVnd.HasValue;
    }

    private static bool ContainsNormalized(string? value, string expected)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var normalizedValue = RemoveVietnameseDiacritics(value).ToLowerInvariant();
        var normalizedExpected = RemoveVietnameseDiacritics(expected).ToLowerInvariant();
        return normalizedValue.Contains(normalizedExpected, StringComparison.Ordinal);
    }

    private static ScrapedJobPosting ToScrapedJobPosting(
        string sourceName,
        MarketPulseIngestPostingDto posting)
    {
        var title = TrimTo(posting.Title, 250) ?? "Untitled IT job";
        var url = TrimTo(posting.Url, 500) ?? string.Empty;
        var description = string.IsNullOrWhiteSpace(posting.Description)
            ? BuildIngestDescription(posting)
            : posting.Description.Trim();

        return new ScrapedJobPosting(
            sourceName,
            title,
            TrimTo(posting.Company, 160),
            TrimTo(posting.Location, 160),
            url,
            description,
            NormalizeUtc(posting.PublishedAt),
            NormalizeUtc(posting.ExpiresAt),
            TrimTo(posting.SourceJobId ?? posting.Id, 120),
            TrimTo(posting.Category, 100),
            TrimTo(posting.Salary, 100),
            TrimTo(posting.Experience, 100),
            TrimTo(posting.PostDateText, 80),
            NormalizeUtc(posting.SourceUpdatedAt),
            CleanStringList(posting.Requirements),
            CleanStringList(posting.Specialties),
            CleanStringList(posting.Benefits),
            CleanStringList(posting.Skills));
    }

    private static string BuildIngestDescription(MarketPulseIngestPostingDto posting)
    {
        var parts = new[]
        {
            posting.Title,
            posting.Company,
            posting.Category,
            posting.Salary,
            posting.Experience,
            posting.Location,
            string.Join(' ', CleanStringList(posting.Skills)),
            string.Join(' ', CleanStringList(posting.Requirements)),
            string.Join(' ', CleanStringList(posting.Specialties)),
            string.Join(' ', CleanStringList(posting.Benefits))
        };

        return string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    }
    
    public async Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken)
    {
        var rawPostings = await scraper.ScrapeAsync(cancellationToken);
        return await PersistScrapedPostingsAsync(rawPostings, cancellationToken);
    }

    public async Task<MarketPulseRefreshResultDto> IngestAsync(
        MarketPulseIngestRequestDto request,
        CancellationToken cancellationToken)
    {
        var sourceName = string.IsNullOrWhiteSpace(request.SourceName)
            ? "Jobs API"
            : request.SourceName.Trim();
        var rawPostings = request.Postings
            .Where(x => x.IsActive)
            .Select(x => ToScrapedJobPosting(sourceName, x))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        return await PersistScrapedPostingsAsync(rawPostings, cancellationToken);
    }

    private async Task<MarketPulseRefreshResultDto> PersistScrapedPostingsAsync(
        IReadOnlyList<ScrapedJobPosting> rawPostings,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var snapshotDate = DateOnly.FromDateTime(now);
        var settings = options.Value;

        if (rawPostings.Count == 0)
        {
            return new MarketPulseRefreshResultDto
            {
                SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
                SourcesScraped = 0,
                PostingsScraped = 0,
                PostingsSaved = 0,
                PostingsInserted = 0,
                PostingsUpdated = 0,
                PostingsSeen = 0,
                PostingsExpired = 0,
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
        var seenPostings = 0;
        var expiredPostingsInRun = 0;
        var missingThreshold = Math.Max(1, settings.MissingScansBeforeStale);
        var minimumLifecyclePostings = Math.Max(1, settings.MinimumPostingsForLifecycleCheck);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var sourceGroup in rawPostings.GroupBy(x => x.SourceName))
        {
            var sourceSettings = settings.Sources.FirstOrDefault(x =>
                string.Equals(x.Name, sourceGroup.Key, StringComparison.OrdinalIgnoreCase));

            var source = await UpsertSourceAsync(sourceGroup.Key, sourceSettings, now, cancellationToken);
            var uniquePostings = sourceGroup
                .GroupBy(x => BuildExternalId(source.Name, x), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            var lookupExternalIds = uniquePostings
                .SelectMany(x => new[]
                {
                    BuildExternalId(source.Name, x),
                    BuildLegacyExternalId(source.Name, x.Url)
                })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingPostings = await dbContext.Set<JobPosting>()
                .Where(x => x.JobPortalSourceId == source.JobPortalSourceId && lookupExternalIds.Contains(x.ExternalId))
                .ToDictionaryAsync(x => x.ExternalId, cancellationToken);

            var observations = new List<PostingObservationItem>();

            foreach (var rawPosting in uniquePostings)
            {
                var externalId = BuildExternalId(source.Name, rawPosting);
                var publishedAt = NormalizeUtc(rawPosting.PublishedAt);
                var expiresAt = NormalizeUtc(rawPosting.ExpiresAt);
                var contentHash = BuildContentHash(rawPosting, expiresAt);
                var isExpired = expiresAt.HasValue && DateOnly.FromDateTime(expiresAt.Value) < snapshotDate;

                if (!existingPostings.TryGetValue(externalId, out var posting))
                {
                    var legacyExternalId = BuildLegacyExternalId(source.Name, rawPosting.Url);
                    existingPostings.TryGetValue(legacyExternalId, out posting);
                }

                if (posting is not null)
                {
                    var changed = !string.Equals(posting.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase);
                    var wasExpired = string.Equals(posting.LifecycleStatus, LifecycleExpired, StringComparison.OrdinalIgnoreCase);

                    if (!existingPostings.ContainsKey(externalId))
                    {
                        posting.ExternalId = externalId;
                        existingPostings[externalId] = posting;
                    }

                    posting.Title = TrimTo(rawPosting.Title, 250) ?? "Untitled IT job";
                    posting.CompanyName = TrimTo(rawPosting.CompanyName, 160);
                    posting.SourceJobId = TrimTo(rawPosting.SourceJobId, 120);
                    posting.Category = TrimTo(rawPosting.Category, 100);
                    posting.Location = TrimTo(rawPosting.Location, 160);
                    posting.Salary = TrimTo(rawPosting.Salary, 100);
                    posting.Experience = TrimTo(rawPosting.Experience, 100);
                    posting.Description = rawPosting.Description;
                    posting.PublishedAt = publishedAt;
                    posting.PostDateText = TrimTo(rawPosting.PostDateText, 80);
                    posting.SourceUpdatedAt = NormalizeUtc(rawPosting.SourceUpdatedAt);
                    posting.ExpiresAt = expiresAt;
                    posting.Requirements = SerializeStringList(rawPosting.Requirements);
                    posting.Specialties = SerializeStringList(rawPosting.Specialties);
                    posting.Benefits = SerializeStringList(rawPosting.Benefits);
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
                    else
                    {
                        seenPostings++;
                    }

                    if (isExpired && !wasExpired)
                    {
                        expiredPostingsInRun++;
                    }

                    await SavePostingVersionAsync(posting, rawPosting.Skills, contentHash, now, cancellationToken);
                    await SyncSkillMentionsAsync(posting, source.Name, rawPosting.Skills, snapshotDate, now, cancellationToken);

                    observations.Add(new PostingObservationItem(
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
                    SourceJobId = TrimTo(rawPosting.SourceJobId, 120),
                    Title = TrimTo(rawPosting.Title, 250) ?? "Untitled IT job",
                    CompanyName = TrimTo(rawPosting.CompanyName, 160),
                    Category = TrimTo(rawPosting.Category, 100),
                    Location = TrimTo(rawPosting.Location, 160),
                    Salary = TrimTo(rawPosting.Salary, 100),
                    Experience = TrimTo(rawPosting.Experience, 100),
                    Url = rawPosting.Url,
                    Description = rawPosting.Description,
                    PublishedAt = publishedAt,
                    PostDateText = TrimTo(rawPosting.PostDateText, 80),
                    SourceUpdatedAt = NormalizeUtc(rawPosting.SourceUpdatedAt),
                    ExpiresAt = expiresAt,
                    Requirements = SerializeStringList(rawPosting.Requirements),
                    Specialties = SerializeStringList(rawPosting.Specialties),
                    Benefits = SerializeStringList(rawPosting.Benefits),
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
                await SavePostingVersionAsync(newPosting, rawPosting.Skills, contentHash, now, cancellationToken);
                await SyncSkillMentionsAsync(newPosting, source.Name, rawPosting.Skills, snapshotDate, now, cancellationToken);
                observations.Add(new PostingObservationItem(newPosting, ObservationNew, contentHash));
                savedPostings++;
                newPostings++;
                if (isExpired)
                {
                    expiredPostingsInRun++;
                }
            }

            await UpsertDailyObservationsAsync(snapshotDate, source.Name, observations, now, cancellationToken);

            if (uniquePostings.Count >= minimumLifecyclePostings)
            {
                expiredPostingsInRun += await MarkMissingPostingsAsync(
                    source.JobPortalSourceId,
                    source.Name,
                    lookupExternalIds,
                    snapshotDate,
                    now,
                    missingThreshold,
                    cancellationToken);
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
        await SaveDailyMarketSnapshotsAsync(snapshotDate, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SaveOverviewInsightAsync(snapshotDate, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new MarketPulseRefreshResultDto
        {
            SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
            SourcesScraped = rawPostings.Select(x => x.SourceName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            PostingsScraped = rawPostings.Count,
            PostingsSaved = savedPostings + updatedPostings + seenPostings,
            PostingsInserted = savedPostings,
            PostingsUpdated = updatedPostings,
            PostingsSeen = seenPostings,
            PostingsExpired = expiredPostingsInRun,
            NewPostings = newPostings,
            UpdatedPostings = updatedPostings,
            ActivePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.IsActive, cancellationToken),
            StalePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken),
            ExpiredPostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken),
            SkillSnapshotsSaved = snapshotCount
        };

        await transaction.CommitAsync(cancellationToken);
        InvalidateOverviewCache();

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
        IReadOnlyCollection<PostingObservationItem> observations,
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
                await UpsertPostingObservationAsync(
                    observation.Posting,
                    snapshotDate,
                    sourceName,
                    observation.Status,
                    observation.ContentHash,
                    observation.Posting.IsActive,
                    now,
                    cancellationToken);
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
            await UpsertPostingObservationAsync(
                observation.Posting,
                snapshotDate,
                sourceName,
                observation.Status,
                observation.ContentHash,
                observation.Posting.IsActive,
                now,
                cancellationToken);
        }
    }

    private async Task UpsertPostingObservationAsync(
        JobPosting posting,
        DateOnly snapshotDate,
        string sourceName,
        string status,
        string contentHash,
        bool isActive,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<JobPostingObservation>()
            .FirstOrDefaultAsync(x =>
                x.JobPostingId == posting.JobPostingId &&
                x.SnapshotDate == snapshotDate &&
                x.ObservationStatus == status,
                cancellationToken);

        if (existing is null)
        {
            dbContext.Set<JobPostingObservation>().Add(new JobPostingObservation
            {
                JobPostingObservationId = Guid.NewGuid(),
                JobPostingId = posting.JobPostingId,
                SnapshotDate = snapshotDate,
                SourceName = sourceName,
                ObservationStatus = status,
                ContentHash = contentHash,
                IsActive = isActive,
                ObservedAt = now,
                CreatedAt = now
            });
            return;
        }

        existing.SourceName = sourceName;
        existing.ContentHash = contentHash;
        existing.IsActive = isActive;
        existing.ObservedAt = now;
    }

    private async Task SavePostingVersionAsync(
        JobPosting posting,
        IEnumerable<string>? skills,
        string contentHash,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Set<JobPostingVersion>()
            .AnyAsync(x =>
                x.JobPostingId == posting.JobPostingId &&
                x.ContentHash == contentHash,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Set<JobPostingVersion>().Add(new JobPostingVersion
        {
            JobPostingVersionId = Guid.NewGuid(),
            JobPostingId = posting.JobPostingId,
            ContentHash = contentHash,
            Title = posting.Title,
            CompanyName = posting.CompanyName,
            Category = posting.Category,
            Location = posting.Location,
            Salary = posting.Salary,
            Experience = posting.Experience,
            Description = posting.Description,
            Requirements = posting.Requirements,
            Specialties = posting.Specialties,
            Benefits = posting.Benefits,
            Skills = SerializeStringList(skills),
            ObservedAt = now,
            CreatedAt = now
        });
    }

    private async Task SyncSkillMentionsAsync(
        JobPosting posting,
        string sourceName,
        IEnumerable<string>? skills,
        DateOnly snapshotDate,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var cleanSkills = CleanStringList(skills);

        var existingMentions = await dbContext.Set<JobSkillMention>()
            .Where(x => x.JobPostingId == posting.JobPostingId)
            .ToListAsync(cancellationToken);

        if (existingMentions.Count > 0)
        {
            dbContext.Set<JobSkillMention>().RemoveRange(existingMentions);
        }

        foreach (var skill in cleanSkills)
        {
            var slug = Slugify(skill);
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            var taxonomy = await UpsertSkillTaxonomyAsync(skill, slug, now, cancellationToken);
            dbContext.Set<JobSkillMention>().Add(new JobSkillMention
            {
                JobSkillMentionId = Guid.NewGuid(),
                JobPostingId = posting.JobPostingId,
                SkillTaxonomyId = taxonomy.SkillTaxonomyId,
                SourceName = sourceName,
                SkillName = taxonomy.SkillName,
                SkillSlug = taxonomy.SkillSlug,
                MentionSource = "normalized",
                SnapshotDate = snapshotDate,
                ObservedAt = now,
                CreatedAt = now
            });
        }
    }

    private async Task<SkillTaxonomy> UpsertSkillTaxonomyAsync(
        string skillName,
        string skillSlug,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var localTaxonomy = dbContext.Set<SkillTaxonomy>()
            .Local
            .FirstOrDefault(x => string.Equals(x.SkillSlug, skillSlug, StringComparison.OrdinalIgnoreCase));
        if (localTaxonomy is not null)
        {
            return localTaxonomy;
        }

        var taxonomy = await dbContext.Set<SkillTaxonomy>()
            .FirstOrDefaultAsync(x => x.SkillSlug == skillSlug, cancellationToken);

        if (taxonomy is not null)
        {
            taxonomy.SkillName = string.IsNullOrWhiteSpace(taxonomy.SkillName)
                ? skillName
                : taxonomy.SkillName;
            taxonomy.IsActive = true;
            taxonomy.UpdatedAt = now;
            return taxonomy;
        }

        taxonomy = new SkillTaxonomy
        {
            SkillTaxonomyId = Guid.NewGuid(),
            SkillName = skillName,
            SkillSlug = skillSlug,
            Aliases = SerializeStringList(new[] { skillName }),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Set<SkillTaxonomy>().Add(taxonomy);
        return taxonomy;
    }

    private async Task<int> MarkMissingPostingsAsync(
        Guid sourceId,
        string sourceName,
        IReadOnlySet<string> observedExternalIds,
        DateOnly snapshotDate,
        DateTime now,
        int missingThreshold,
        CancellationToken cancellationToken)
    {
        var expiredInRun = 0;
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
                var wasExpired = string.Equals(posting.LifecycleStatus, LifecycleExpired, StringComparison.OrdinalIgnoreCase);
                posting.IsActive = false;
                posting.LifecycleStatus = LifecycleExpired;
                posting.ClosedDetectedAt ??= now;
                if (!wasExpired)
                {
                    expiredInRun++;
                }
                await UpsertPostingObservationAsync(
                    posting,
                    snapshotDate,
                    sourceName,
                    LifecycleExpired,
                    posting.ContentHash,
                    false,
                    now,
                    cancellationToken);
            }
            else if (posting.MissingScanCount >= missingThreshold)
            {
                posting.IsActive = false;
                posting.LifecycleStatus = LifecycleStaleUnverified;
                await UpsertPostingObservationAsync(
                    posting,
                    snapshotDate,
                    sourceName,
                    LifecycleStaleUnverified,
                    posting.ContentHash,
                    false,
                    now,
                    cancellationToken);
            }

            posting.UpdatedAt = now;
        }

        return expiredInRun;
    }

    private async Task SaveDailyMarketSnapshotsAsync(
        DateOnly snapshotDate,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var activePostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var mentions = await dbContext.Set<JobSkillMention>()
            .AsNoTracking()
            .Where(x => x.SnapshotDate == snapshotDate)
            .ToListAsync(cancellationToken);

        var existingRows = await dbContext.Set<JobMarketDailySnapshot>()
            .Where(x => x.SnapshotDate == snapshotDate && x.SourceName == AggregateSourceName)
            .ToListAsync(cancellationToken);

        if (existingRows.Count > 0)
        {
            dbContext.Set<JobMarketDailySnapshot>().RemoveRange(existingRows);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var rows = new List<JobMarketDailySnapshot>();
        AddDailySnapshotRow(rows, snapshotDate, now, activePostings);

        foreach (var group in activePostings.GroupBy(x => x.Category ?? "Other"))
        {
            AddDailySnapshotRow(rows, snapshotDate, now, group.ToList(), category: group.Key);
        }

        foreach (var group in activePostings.GroupBy(x => x.Location ?? "Unknown"))
        {
            AddDailySnapshotRow(rows, snapshotDate, now, group.ToList(), location: group.Key);
        }

        var postingsById = activePostings.ToDictionary(x => x.JobPostingId);
        foreach (var group in mentions.GroupBy(x => new { x.SkillSlug, x.SkillName }))
        {
            var postings = group
                .Select(x => postingsById.TryGetValue(x.JobPostingId, out var posting) ? posting : null)
                .OfType<JobPosting>()
                .DistinctBy(x => x.JobPostingId)
                .ToList();

            AddDailySnapshotRow(
                rows,
                snapshotDate,
                now,
                postings,
                skillSlug: group.Key.SkillSlug,
                skillName: group.Key.SkillName,
                mentionCount: group.Count());
        }

        if (rows.Count > 0)
        {
            dbContext.Set<JobMarketDailySnapshot>().AddRange(rows);
        }
    }

    private static void AddDailySnapshotRow(
        ICollection<JobMarketDailySnapshot> rows,
        DateOnly snapshotDate,
        DateTime now,
        IReadOnlyCollection<JobPosting> postings,
        string? category = null,
        string? location = null,
        string? skillSlug = null,
        string? skillName = null,
        int mentionCount = 0)
    {
        var postingsWithSalary = postings
            .Where(x => !string.IsNullOrWhiteSpace(x.Salary))
            .ToList();

        rows.Add(new JobMarketDailySnapshot
        {
            JobMarketDailySnapshotId = Guid.NewGuid(),
            SnapshotDate = snapshotDate,
            SourceName = AggregateSourceName,
            Category = category,
            Location = location,
            SkillSlug = skillSlug,
            SkillName = skillName,
            ActiveJobCount = postings.Count(x => x.IsActive),
            NewJobCount = postings.Count(x =>
                x.PublishedAt.HasValue &&
                DateOnly.FromDateTime(x.PublishedAt.Value) == snapshotDate),
            ObservedJobCount = postings.Count,
            MentionCount = mentionCount,
            SalarySampleCount = postingsWithSalary.Count,
            SampleSize = postings.Count,
            Confidence = ConfidenceForSample(postings.Count),
            GeneratedAt = now,
            CreatedAt = now
        });
    }

    private async Task SaveOverviewInsightAsync(
        DateOnly snapshotDate,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var overview = await dbContext.Set<JobMarketDailySnapshot>()
            .FirstOrDefaultAsync(x =>
                x.SnapshotDate == snapshotDate &&
                x.SourceName == AggregateSourceName &&
                x.Category == null &&
                x.Location == null &&
                x.SkillSlug == null,
                cancellationToken);

        if (overview is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            overview.ActiveJobCount,
            overview.NewJobCount,
            overview.ObservedJobCount,
            overview.SampleSize,
            overview.Confidence,
            GeneratedAt = now
        });

        var existing = await dbContext.Set<MarketPulseInsightSnapshot>()
            .FirstOrDefaultAsync(x =>
                x.SnapshotDate == snapshotDate &&
                x.SourceName == AggregateSourceName &&
                x.InsightKey == "market-overview",
                cancellationToken);

        if (existing is null)
        {
            dbContext.Set<MarketPulseInsightSnapshot>().Add(new MarketPulseInsightSnapshot
            {
                MarketPulseInsightSnapshotId = Guid.NewGuid(),
                SnapshotDate = snapshotDate,
                SourceName = AggregateSourceName,
                InsightKey = "market-overview",
                InsightType = "overview",
                PeriodDays = 1,
                SampleSize = overview.SampleSize,
                Confidence = overview.Confidence,
                Payload = payload,
                GeneratedAt = now,
                CreatedAt = now
            });
            return;
        }

        existing.SampleSize = overview.SampleSize;
        existing.Confidence = overview.Confidence;
        existing.Payload = payload;
        existing.GeneratedAt = now;
    }

    private async Task<int> SaveSnapshotsAsync(
        DateOnly snapshotDate,
        IReadOnlyCollection<JobMarketKeywordFrequency> frequencies,
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

    private static SalaryRange? TryParseMonthlySalary(string? salary)
    {
        if (string.IsNullOrWhiteSpace(salary))
        {
            return null;
        }

        var text = RemoveVietnameseDiacritics(salary.Trim().ToLowerInvariant());
        if (text.Contains("thoa thuan", StringComparison.Ordinal) ||
            text.Contains("thuong luong", StringComparison.Ordinal) ||
            text.Contains("negotiable", StringComparison.Ordinal) ||
            text.Contains("canh tranh", StringComparison.Ordinal))
        {
            return null;
        }

        var values = Regex.Matches(text, @"\d+(?:[.,]\d+)?")
            .Select(match => decimal.TryParse(
                match.Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : (decimal?)null)
            .OfType<decimal>()
            .Where(x => x > 0)
            .ToList();

        if (values.Count == 0)
        {
            return null;
        }

        var multiplier = text.Contains("usd", StringComparison.Ordinal) || text.Contains('$', StringComparison.Ordinal)
            ? 25_000m
            : text.Contains("trieu", StringComparison.Ordinal) || text.Contains("tr", StringComparison.Ordinal) || values.Max() < 1_000m
                ? 1_000_000m
                : 1m;
        var normalized = values.Select(x => x * multiplier).OrderBy(x => x).ToList();

        return normalized.Count == 1
            ? new SalaryRange(normalized[0], normalized[0])
            : new SalaryRange(normalized.First(), normalized.Last());
    }

    private static string RemoveVietnameseDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('\u0111', 'd')
            .Replace('\u0110', 'D');
    }

    private static string BuildExternalId(string sourceName, ScrapedJobPosting posting)
    {
        if (!string.IsNullOrWhiteSpace(posting.SourceJobId))
        {
            var sourceJobId = posting.SourceJobId.Trim();
            return sourceJobId.Length <= 120
                ? sourceJobId
                : HashIdentity($"{sourceName}:source_job_id:{sourceJobId}");
        }

        var normalizedUrl = NormalizeUrlForIdentity(posting.Url);
        return HashIdentity($"{sourceName}:url:{normalizedUrl}");
    }

    private static string BuildLegacyExternalId(string sourceName, string url)
    {
        return HashIdentity($"{sourceName}:{url.Trim()}");
    }

    private static string HashIdentity(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeUrlForIdentity(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = uri.AbsolutePath.TrimEnd('/')
        };

        return builder.Uri.ToString();
    }

    private static string BuildContentHash(ScrapedJobPosting posting, DateTime? expiresAt)
    {
        var normalized = string.Join('\n',
            NormalizeForHash(posting.Title),
            NormalizeForHash(posting.SourceJobId),
            NormalizeForHash(posting.CompanyName),
            NormalizeForHash(posting.Category),
            NormalizeForHash(posting.Location),
            NormalizeForHash(posting.Salary),
            NormalizeForHash(posting.Experience),
            NormalizeForHash(posting.Description),
            NormalizeForHash(posting.PostDateText),
            NormalizeForHash(posting.SourceUpdatedAt?.ToString("O")),
            SerializeStringList(posting.Requirements),
            SerializeStringList(posting.Specialties),
            SerializeStringList(posting.Benefits),
            SerializeStringList(posting.Skills),
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

    private static IReadOnlyList<string> CleanStringList(IEnumerable<string>? values)
    {
        return values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static IReadOnlyList<string> DeserializeStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) is { Count: > 0 } values
                ? CleanStringList(values)
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeStringList(IEnumerable<string>? values)
    {
        return JsonSerializer.Serialize(CleanStringList(values));
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("#", "sharp", StringComparison.Ordinal)
            .Replace("+", "plus", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-");
        return normalized.Trim('-');
    }

    private static string ConfidenceForSample(int sampleSize)
    {
        if (sampleSize >= 100)
        {
            return "high";
        }

        return sampleSize >= 30 ? "medium" : "low";
    }
}

internal sealed record PostingObservationItem(JobPosting Posting, string Status, string ContentHash);

internal sealed record SalaryRange(decimal? MinMonthlyVnd, decimal? MaxMonthlyVnd);
