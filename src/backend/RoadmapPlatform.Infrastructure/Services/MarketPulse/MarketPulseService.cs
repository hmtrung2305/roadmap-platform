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
    IMemoryCache cache,
    IOptions<MarketPulseSettings> options) : IMarketPulseService
{
    private const string DefaultSourceName = "topcv";
    private const string OverviewCacheVersionKey = "market-pulse:overview:version";
    private const string LifecycleActive = "active";
    private const string LifecycleStaleUnverified = "stale_unverified";
    private const string LifecycleExpired = "expired";
    private const string LifecycleSkippedPartialSync = "partial_sync";
    private const string LifecycleSkippedInvalidFreshness = "source_freshness_invalid";
    private const string LifecycleSkippedIneligibleFetchStatus = "fetch_status_not_eligible";
    private const string LifecycleSkippedBelowMinimum = "below_minimum_posting_threshold";
    private const string LifecycleSkippedManualIngest = "manual_ingest_without_complete_sync_metadata";
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

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
            SalaryRaw = posting.SalaryRaw,
            SalaryMin = posting.SalaryMin,
            SalaryMax = posting.SalaryMax,
            SalaryCurrency = posting.SalaryCurrency,
            SalaryIsNegotiable = posting.SalaryIsNegotiable,
            Experience = posting.Experience,
            ExperienceRaw = posting.ExperienceRaw,
            ExperienceMinYears = posting.ExperienceMinYears,
            ExperienceMaxYears = posting.ExperienceMaxYears,
            PostedOn = posting.PublishedAt.HasValue ? DateOnly.FromDateTime(posting.PublishedAt.Value) : null,
            PostedOnText = posting.PostDateText,
            PostDateConfidence = posting.PostDateConfidence,
            UpdatedAt = posting.SourceUpdatedAt ?? posting.UpdatedAt,
            DetailStatus = posting.DetailStatus,
            DetailLastSuccessAt = posting.DetailLastSuccessAt,
            Url = posting.Url,
            IsActive = posting.IsActive,
            Requirements = DeserializeStringList(posting.Requirements),
            Specialties = DeserializeStringList(posting.Specialties),
            Benefits = DeserializeStringList(posting.Benefits),
            Skills = DeserializeStringList(posting.Skills)
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
            !ContainsNormalized(posting.ExperienceRaw, query.Experience) &&
            !ContainsNormalized(posting.Title, query.Experience))
        {
            return false;
        }

        if (query.SalaryMinMonthlyVnd.HasValue || query.SalaryMaxMonthlyVnd.HasValue)
        {
            var salaryRange = ResolveMonthlySalary(posting);
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
    
    public Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken) =>
        RefreshAsync(null, cancellationToken);

    public async Task<MarketPulseRefreshResultDto> RefreshAsync(
        MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (!await RefreshLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("A Market Pulse refresh is already running.");
        }

        MarketPulseCrawlRun? run = null;

        try
        {
            var startedAt = DateTime.UtcNow;
            run = await StartImportRunAsync("manual", startedAt, cancellationToken);

            var fetchResult = await scraper.ScrapeAsync(request, cancellationToken);
            if (fetchResult.Status is JobsApiFetchStatus.HttpError or
                JobsApiFetchStatus.Timeout or
                JobsApiFetchStatus.InvalidContract or
                JobsApiFetchStatus.StaleSource)
            {
                throw new MarketPulseSourceFetchException(fetchResult);
            }

            var lifecycleDecision = ResolveMissingLifecycleDecision(fetchResult);
            var result = await PersistScrapedPostingsAsync(
                fetchResult.Postings,
                lifecycleDecision,
                cancellationToken);
            result.PostingsScraped = fetchResult.FetchedCount;
            result.SourceTotal = fetchResult.Total;
            result.SourceGeneratedAt = fetchResult.GeneratedAt;
            result.LatestSuccessfulCrawlAt = fetchResult.LatestSuccessfulCrawlAt;
            result.IsCompleteSync = fetchResult.IsCompleteSync;
            result.IsSourceFresh = fetchResult.IsSourceFresh;
            result.FetchStatus = fetchResult.Status.ToString();
            var status = fetchResult.Status == JobsApiFetchStatus.Empty ? "empty" : "success";

            await FinishImportRunAsync(run, result, fetchResult, status, cancellationToken);

            result.RunId = run.MarketPulseCrawlRunId;
            result.StartedAt = run.StartedAt;
            result.FinishedAt = run.FinishedAt;
            result.Status = run.Status;
            result.Mode = run.Mode;

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (run is not null)
            {
                await RecordRefreshFailureAsync(run, ex, cancellationToken);
            }

            throw;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public async Task<MarketPulseRefreshResultDto> IngestAsync(
        MarketPulseIngestRequestDto request,
        CancellationToken cancellationToken)
    {
        var sourceName = string.IsNullOrWhiteSpace(request.SourceName)
            ? "topcv"
            : request.SourceName.Trim();
        var rawPostings = request.Postings
            .Where(x => x.IsActive)
            .Select(x => ToScrapedJobPosting(sourceName, x))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        return await PersistScrapedPostingsAsync(
            rawPostings,
            MissingLifecycleDecision.Skip(LifecycleSkippedManualIngest),
            cancellationToken);
    }

    private async Task<MarketPulseRefreshResultDto> PersistScrapedPostingsAsync(
        IReadOnlyList<ScrapedJobPosting> rawPostings,
        MissingLifecycleDecision lifecycleDecision,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var snapshotDate = DateOnly.FromDateTime(now);
        var settings = options.Value;

        if (rawPostings.Count == 0 && !lifecycleDecision.ShouldApply)
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
                MissingLifecycleApplied = false,
                LifecycleSkippedReason = lifecycleDecision.SkippedReason,
                SkillSnapshotsSaved = 0
            };
        }

        var savedPostings = 0;
        var newPostings = 0;
        var updatedPostings = 0;
        var seenPostings = 0;
        var duplicatePostings = 0;
        var expiredPostingsInRun = 0;
        var missingLifecycleApplied = false;
        var lifecycleSkippedReason = lifecycleDecision.SkippedReason;
        var lifecycleSkippedBelowMinimum = false;
        var missingThreshold = Math.Max(1, settings.MissingScansBeforeStale);
        var minimumLifecyclePostings = Math.Max(1, settings.MinimumPostingsForLifecycleCheck);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var sourceGroup in rawPostings.GroupBy(x => x.SourceName))
        {
            var sourcePostings = sourceGroup.ToList();
            var sourceSettings = settings.Sources.FirstOrDefault(x =>
                string.Equals(x.Name, sourceGroup.Key, StringComparison.OrdinalIgnoreCase));

            var source = await UpsertSourceAsync(sourceGroup.Key, sourceSettings, now, cancellationToken);
            var uniquePostings = sourcePostings
                .GroupBy(x => BuildExternalId(source.Name, x), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            duplicatePostings += Math.Max(0, sourcePostings.Count - uniquePostings.Count);
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
                    posting.SalaryRaw = TrimTo(rawPosting.SalaryRaw, 160);
                    posting.SalaryMin = rawPosting.SalaryMin;
                    posting.SalaryMax = rawPosting.SalaryMax;
                    posting.SalaryCurrency = TrimTo(rawPosting.SalaryCurrency?.ToUpperInvariant(), 16);
                    posting.SalaryIsNegotiable = rawPosting.SalaryIsNegotiable;
                    posting.Experience = TrimTo(rawPosting.Experience, 100);
                    posting.ExperienceRaw = TrimTo(rawPosting.ExperienceRaw, 160);
                    posting.ExperienceMinYears = rawPosting.ExperienceMinYears;
                    posting.ExperienceMaxYears = rawPosting.ExperienceMaxYears;
                    posting.Description = rawPosting.Description;
                    posting.PublishedAt = publishedAt;
                    posting.PostDateText = TrimTo(rawPosting.PostDateText, 80);
                    posting.PostDateConfidence = TrimTo(rawPosting.PostDateConfidence, 20);
                    posting.SourceUpdatedAt = NormalizeUtc(rawPosting.SourceUpdatedAt);
                    posting.DetailStatus = TrimTo(rawPosting.DetailStatus, 32);
                    posting.DetailLastSuccessAt = NormalizeUtc(rawPosting.DetailLastSuccessAt);
                    posting.ExpiresAt = expiresAt;
                    posting.Requirements = SerializeStringList(rawPosting.Requirements);
                    posting.Specialties = SerializeStringList(rawPosting.Specialties);
                    posting.Benefits = SerializeStringList(rawPosting.Benefits);
                    posting.Skills = SerializeStringList(rawPosting.Skills);
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
                    SalaryRaw = TrimTo(rawPosting.SalaryRaw, 160),
                    SalaryMin = rawPosting.SalaryMin,
                    SalaryMax = rawPosting.SalaryMax,
                    SalaryCurrency = TrimTo(rawPosting.SalaryCurrency?.ToUpperInvariant(), 16),
                    SalaryIsNegotiable = rawPosting.SalaryIsNegotiable,
                    Experience = TrimTo(rawPosting.Experience, 100),
                    ExperienceRaw = TrimTo(rawPosting.ExperienceRaw, 160),
                    ExperienceMinYears = rawPosting.ExperienceMinYears,
                    ExperienceMaxYears = rawPosting.ExperienceMaxYears,
                    Url = rawPosting.Url,
                    Description = rawPosting.Description,
                    PublishedAt = publishedAt,
                    PostDateText = TrimTo(rawPosting.PostDateText, 80),
                    PostDateConfidence = TrimTo(rawPosting.PostDateConfidence, 20),
                    SourceUpdatedAt = NormalizeUtc(rawPosting.SourceUpdatedAt),
                    DetailStatus = TrimTo(rawPosting.DetailStatus, 32),
                    DetailLastSuccessAt = NormalizeUtc(rawPosting.DetailLastSuccessAt),
                    ExpiresAt = expiresAt,
                    Requirements = SerializeStringList(rawPosting.Requirements),
                    Specialties = SerializeStringList(rawPosting.Specialties),
                    Benefits = SerializeStringList(rawPosting.Benefits),
                    Skills = SerializeStringList(rawPosting.Skills),
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
                savedPostings++;
                newPostings++;
                if (isExpired)
                {
                    expiredPostingsInRun++;
                }
            }

            if (lifecycleDecision.ShouldApply && uniquePostings.Count >= minimumLifecyclePostings)
            {
                expiredPostingsInRun += await MarkMissingPostingsAsync(
                    source.JobPortalSourceId,
                    lookupExternalIds,
                    snapshotDate,
                    now,
                    missingThreshold,
                    cancellationToken);
                missingLifecycleApplied = true;
            }
            else if (lifecycleDecision.ShouldApply)
            {
                lifecycleSkippedBelowMinimum = true;
            }
        }

        if (lifecycleDecision.ShouldApply && rawPostings.Count == 0)
        {
            var sourceSettings = settings.Sources.FirstOrDefault(x =>
                string.Equals(x.Name, DefaultSourceName, StringComparison.OrdinalIgnoreCase));
            var source = await UpsertSourceAsync(
                DefaultSourceName,
                sourceSettings,
                now,
                cancellationToken);
            expiredPostingsInRun += await MarkMissingPostingsAsync(
                source.JobPortalSourceId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                snapshotDate,
                now,
                missingThreshold,
                cancellationToken);
            missingLifecycleApplied = true;
        }

        if (!missingLifecycleApplied && lifecycleSkippedBelowMinimum)
        {
            lifecycleSkippedReason = LifecycleSkippedBelowMinimum;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new MarketPulseRefreshResultDto
        {
            SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
            SourcesScraped = rawPostings.Select(x => x.SourceName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            PostingsScraped = rawPostings.Count,
            PostingsSaved = savedPostings + updatedPostings + seenPostings,
            PostingsDuplicated = duplicatePostings,
            PostingsFailed = 0,
            PostingsInserted = savedPostings,
            PostingsUpdated = updatedPostings,
            PostingsSeen = seenPostings,
            PostingsExpired = expiredPostingsInRun,
            NewPostings = newPostings,
            UpdatedPostings = updatedPostings,
            ActivePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.IsActive, cancellationToken),
            StalePostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken),
            ExpiredPostings = await dbContext.Set<JobPosting>().CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken),
            MissingLifecycleApplied = missingLifecycleApplied,
            LifecycleSkippedReason = missingLifecycleApplied ? null : lifecycleSkippedReason,
            SkillSnapshotsSaved = 0
        };

        await transaction.CommitAsync(cancellationToken);
        InvalidateOverviewCache();

        return result;
    }

    private MissingLifecycleDecision ResolveMissingLifecycleDecision(JobPortalScrapeResult fetchResult)
    {
        if (fetchResult.Status is not JobsApiFetchStatus.Success and not JobsApiFetchStatus.Empty)
        {
            return MissingLifecycleDecision.Skip(LifecycleSkippedIneligibleFetchStatus);
        }

        if (!fetchResult.IsSourceFresh)
        {
            return MissingLifecycleDecision.Skip(LifecycleSkippedInvalidFreshness);
        }

        if (!fetchResult.IsCompleteSync && options.Value.DisableMissingLifecycleForPartialSync)
        {
            return MissingLifecycleDecision.Skip(LifecycleSkippedPartialSync);
        }

        return MissingLifecycleDecision.Apply();
    }

    private async Task<MarketPulseCrawlRun> StartImportRunAsync(
        string triggerType,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var run = new MarketPulseCrawlRun
        {
            MarketPulseCrawlRunId = Guid.NewGuid(),
            SourceName = DefaultSourceName,
            Status = "running",
            Mode = "jobs_api_pull",
            TriggerType = triggerType,
            StartedAt = startedAt,
            CreatedAt = startedAt
        };

        dbContext.Set<MarketPulseCrawlRun>().Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    private async Task FinishImportRunAsync(
        MarketPulseCrawlRun run,
        MarketPulseRefreshResultDto result,
        JobPortalScrapeResult fetchResult,
        string status,
        CancellationToken cancellationToken)
    {
        var finishedAt = DateTime.UtcNow;
        run.Status = status;
        run.FinishedAt = finishedAt;
        run.DurationMs = Math.Max(0, (int)Math.Round((finishedAt - run.StartedAt).TotalMilliseconds));
        run.FetchedCount = fetchResult.FetchedCount;
        run.SavedCount = result.PostingsSaved;
        run.ImportedCount = result.PostingsInserted;
        run.UpdatedCount = result.PostingsUpdated;
        run.SkippedCount = result.PostingsDuplicated;
        run.DuplicateCount = result.PostingsDuplicated;
        run.FailedCount = result.PostingsFailed;
        run.StoppedReason = status is "success" or "empty" ? null : status;
        run.SourceTotalCount = fetchResult.Total;
        run.IsCompleteSync = fetchResult.IsCompleteSync;
        run.SourceGeneratedAt = fetchResult.GeneratedAt?.UtcDateTime;
        run.SourceLatestSuccessAt = fetchResult.LatestSuccessfulCrawlAt?.UtcDateTime;
        run.MissingLifecycleApplied = result.MissingLifecycleApplied;
        run.LifecycleSkippedReason = result.LifecycleSkippedReason;
        run.ErrorSummary = fetchResult.ErrorMessage;

        await UpsertSourceHealthAsync(
            DefaultSourceName,
            run,
            status,
            fetchResult.ErrorMessage,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordRefreshFailureAsync(
        MarketPulseCrawlRun run,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var errorSummary = TrimTo(exception.Message, 1000);
        var fetchException = exception as MarketPulseSourceFetchException;
        var failureStatus = fetchException?.FetchResult.Status == JobsApiFetchStatus.StaleSource
            ? "stale_source"
            : "failed";

        run.Status = failureStatus;
        run.FinishedAt = now;
        run.DurationMs = Math.Max(0, (int)Math.Round((now - run.StartedAt).TotalMilliseconds));
        run.FailedCount = Math.Max(1, run.FailedCount);
        run.StoppedReason = fetchException?.FetchResult.Status.ToString() ?? "exception";
        run.MissingLifecycleApplied = false;
        run.LifecycleSkippedReason = fetchException?.FetchResult.Status == JobsApiFetchStatus.StaleSource
            ? LifecycleSkippedInvalidFreshness
            : LifecycleSkippedIneligibleFetchStatus;
        run.ErrorSummary = errorSummary;

        if (fetchException is not null)
        {
            run.FetchedCount = fetchException.FetchResult.FetchedCount;
            run.SourceTotalCount = fetchException.FetchResult.Total;
            run.IsCompleteSync = fetchException.FetchResult.IsCompleteSync;
            run.SourceGeneratedAt = fetchException.FetchResult.GeneratedAt?.UtcDateTime;
            run.SourceLatestSuccessAt = fetchException.FetchResult.LatestSuccessfulCrawlAt?.UtcDateTime;
        }

        dbContext.Set<MarketPulseFailedItem>().Add(new MarketPulseFailedItem
        {
            MarketPulseFailedItemId = Guid.NewGuid(),
            MarketPulseCrawlRunId = run.MarketPulseCrawlRunId,
            SourceName = DefaultSourceName,
            Stage = "import",
            ErrorCode = fetchException?.FetchResult.Status.ToString() ?? exception.GetType().Name,
            ErrorMessage = errorSummary ?? "Market Pulse refresh failed.",
            ErrorDetail = TrimTo(exception.ToString(), 4000),
            RetryCount = 0,
            Status = "open",
            CreatedAt = now,
            UpdatedAt = now
        });

        await UpsertSourceHealthAsync(DefaultSourceName, run, failureStatus, errorSummary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSourceHealthAsync(
        string sourceName,
        MarketPulseCrawlRun run,
        string status,
        string? errorSummary,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var health = await dbContext.Set<MarketPulseSourceHealth>()
            .FirstOrDefaultAsync(x => x.SourceName == sourceName, cancellationToken);

        if (health is null)
        {
            health = new MarketPulseSourceHealth
            {
                MarketPulseSourceHealthId = Guid.NewGuid(),
                SourceName = sourceName,
                UpdatedAt = now
            };
            dbContext.Set<MarketPulseSourceHealth>().Add(health);
        }

        health.Status = status;
        health.LastRunId = run.MarketPulseCrawlRunId;
        health.LastErrorSummary = errorSummary;
        health.UpdatedAt = now;
        if (run.SourceGeneratedAt.HasValue)
        {
            health.SourceGeneratedAt = run.SourceGeneratedAt;
        }

        if (run.SourceLatestSuccessAt.HasValue)
        {
            health.SourceLatestSuccessAt = run.SourceLatestSuccessAt;
        }

        if (status is "success" or "empty")
        {
            health.LastSuccessAt = run.FinishedAt ?? now;
            health.ConsecutiveFailures = 0;
            return;
        }

        health.LastFailureAt = run.FinishedAt ?? now;
        health.ConsecutiveFailures++;
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

    private async Task<int> MarkMissingPostingsAsync(
        Guid sourceId,
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
            }
            else if (posting.MissingScanCount >= missingThreshold)
            {
                posting.IsActive = false;
                posting.LifecycleStatus = LifecycleStaleUnverified;
            }

            posting.UpdatedAt = now;
        }

        return expiredInRun;
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

    private static SalaryRange? ResolveMonthlySalary(JobMarketPosting posting)
    {
        if (posting.SalaryMin.HasValue || posting.SalaryMax.HasValue)
        {
            var multiplier = posting.SalaryCurrency?.Trim().ToUpperInvariant() switch
            {
                "VND" => 1m,
                "USD" => 25_000m,
                _ => 0m
            };

            if (multiplier > 0)
            {
                return new SalaryRange(
                    posting.SalaryMin * multiplier,
                    posting.SalaryMax * multiplier);
            }
        }

        return TryParseMonthlySalary(posting.Salary ?? posting.SalaryRaw);
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

    private sealed record MissingLifecycleDecision(bool ShouldApply, string? SkippedReason)
    {
        public static MissingLifecycleDecision Apply() => new(true, null);

        public static MissingLifecycleDecision Skip(string reason) => new(false, reason);
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
            NormalizeForHash(posting.SalaryRaw),
            NormalizeForHash(posting.SalaryMin?.ToString(CultureInfo.InvariantCulture)),
            NormalizeForHash(posting.SalaryMax?.ToString(CultureInfo.InvariantCulture)),
            NormalizeForHash(posting.SalaryCurrency),
            NormalizeForHash(posting.SalaryIsNegotiable?.ToString()),
            NormalizeForHash(posting.Experience),
            NormalizeForHash(posting.ExperienceRaw),
            NormalizeForHash(posting.ExperienceMinYears?.ToString(CultureInfo.InvariantCulture)),
            NormalizeForHash(posting.ExperienceMaxYears?.ToString(CultureInfo.InvariantCulture)),
            NormalizeForHash(posting.Description),
            NormalizeForHash(posting.PostDateText),
            NormalizeForHash(posting.PostDateConfidence),
            NormalizeForHash(posting.SourceUpdatedAt?.ToString("O")),
            NormalizeForHash(posting.DetailStatus),
            NormalizeForHash(posting.DetailLastSuccessAt?.ToString("O")),
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

internal sealed record SalaryRange(decimal? MinMonthlyVnd, decimal? MaxMonthlyVnd);

internal sealed class MarketPulseSourceFetchException(JobPortalScrapeResult fetchResult)
    : Exception(fetchResult.ErrorMessage ?? $"Jobs API fetch failed with status {fetchResult.Status}.")
{
    public JobPortalScrapeResult FetchResult { get; } = fetchResult;
}
