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
    TopCvJobsApiClient topCvClient,
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
    private const string LifecycleSkippedOutdatedObservation = "source_observation_not_newer";
    private const long ImportAdvisoryLockKey = 5_067_548_361_047_343_941L;
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
        var today = MarketPulseBusinessTime.GetBusinessDate(now, settings.BusinessTimezone);
        var sourceFilter = NormalizeFilter(query.Source);
        if (sourceFilter is not null &&
            !string.Equals(sourceFilter, DefaultSourceName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Market Pulse only supports source='{DefaultSourceName}'.",
                nameof(query.Source));
        }
        var categoryFilter = NormalizeFilter(query.Category);
        var locationFilter = NormalizeFilter(query.Location);
        var normalizedCategoryFilter = categoryFilter?.ToLowerInvariant();
        var normalizedLocationFilter = locationFilter?.ToLowerInvariant();

        var stalePostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken);

        var expiredPostings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken);

        var postings = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => normalizedCategoryFilter == null ||
                (x.Category != null && x.Category.ToLower() == normalizedCategoryFilter))
            .Where(x => normalizedLocationFilter == null ||
                (x.Location != null && x.Location.ToLower() == normalizedLocationFilter))
            .OrderByDescending(x => x.PublishedAt ?? x.LastSeenAt)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        var activeJobs = postings
            .Select(x => ToJobMarketPosting(x, settings.BusinessTimezone))
            .Where(x => MatchesMemoryFilters(x, query))
            .ToList();
        var todayJobs = activeJobs
            .Where(x =>
                x.PostedOn == today &&
                MarketPulseBusinessTime.IsReliablePostDate(x.PostDateConfidence))
            .ToList();
        var overview = overviewBuilder.Build(
            new JobMarketSnapshot
            {
                ActiveTotal = activeJobs.Count,
                TodayTotal = todayJobs.Count,
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
        overview.ObservationAnalytics = null;
        overview.PublicationAnalytics = await BuildPublicationAnalyticsAsync(
            query,
            today,
            cancellationToken);
        ApplyPublicationSkillInsights(overview, query.Days);
        return overview;
    }

    private static void ApplyPublicationSkillInsights(MarketPulseOverviewDto overview, int days)
    {
        var analytics = overview.PublicationAnalytics;
        if (!string.Equals(analytics.Availability, "available", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        static MarketSkillMovementDto ToMovement(
            MarketPulsePublicationSkillComparisonDto skill,
            int periodDays,
            int sampleSize) => new()
        {
            SkillName = skill.SkillName,
            SkillSlug = skill.SkillSlug,
            CurrentMentions = (int)Math.Round(skill.CurrentTotal ?? 0),
            PreviousMentions = (int)Math.Round(skill.PreviousTotal ?? 0),
            Delta = (int)Math.Round(skill.Delta ?? 0),
            GrowthPercent = skill.GrowthPercent ?? 0,
            SampleSize = sampleSize,
            PeriodDays = periodDays,
            Confidence = skill.Confidence
        };

        var sampleSize = analytics.PostDateQuality.SampleSize;
        overview.RisingSkills = analytics.SkillComparisons
            .Where(skill => skill.Direction is "up" or "new")
            .OrderByDescending(skill => skill.Delta ?? skill.CurrentTotal ?? 0)
            .Select(skill => ToMovement(skill, days, sampleSize))
            .ToList();
        overview.FallingSkills = analytics.SkillComparisons
            .Where(skill => skill.Direction == "down")
            .OrderBy(skill => skill.Delta ?? 0)
            .Select(skill => ToMovement(skill, days, sampleSize))
            .ToList();

        var recommendationSkills = analytics.SkillComparisons
            .Where(skill => (skill.CurrentTotal ?? 0) > 0)
            .OrderByDescending(skill => skill.Direction is "up" or "new")
            .ThenByDescending(skill => skill.Delta ?? skill.CurrentTotal ?? 0)
            .Take(3)
            .ToList();
        overview.LearningRecommendations = recommendationSkills.Select(skill =>
            new MarketLearningRecommendationDto
            {
                Title = $"Prioritize {skill.SkillName}",
                Detail = $"Estimated TopCV demand is {skill.CurrentTotal:0.##} postings versus {skill.PreviousTotal:0.##} in the previous {days}-day period.",
                ActionLabel = "View skill demand",
                SkillSlug = skill.SkillSlug,
                Priority = skill.Confidence == "high" ? "high" : "medium",
                SampleSize = sampleSize,
                Confidence = skill.Confidence
            }).ToList();
    }

    private async Task<MarketPulsePublicationAnalyticsDto> BuildPublicationAnalyticsAsync(
        MarketPulseOverviewQueryDto query,
        DateOnly fallbackAnchor,
        CancellationToken cancellationToken)
    {
        var sourceDataAt = await dbContext.Set<MarketPulseCrawlRun>()
            .AsNoTracking()
            .Where(run =>
                (run.Status == "success" || run.Status == "empty" || run.Status == "partial") &&
                run.SourceLatestSuccessAt.HasValue)
            .OrderByDescending(run => run.SourceLatestSuccessAt)
            .Select(run => run.SourceLatestSuccessAt)
            .FirstOrDefaultAsync(cancellationToken);
        var history = await dbContext.Set<MarketPulsePublicationHistoryState>()
            .AsNoTracking()
            .SingleOrDefaultAsync(state => state.SingletonId == 1, cancellationToken);
        if (history is not null &&
            (!sourceDataAt.HasValue || history.SourceDataAt > sourceDataAt.Value))
        {
            sourceDataAt = history.SourceDataAt;
        }
        // Publication demand is always compared with the current Vietnam calendar
        // date. Crawler timestamps describe freshness only; hour/minute differences
        // never move the analytics window.
        var anchor = fallbackAnchor;
        var category = NormalizeFilter(query.Category)?.ToLowerInvariant();
        var location = NormalizeFilter(query.Location)?.ToLowerInvariant();
        var rows = await dbContext.Set<JobPosting>()
            .AsNoTracking()
            .Where(posting => category == null ||
                (posting.Category != null && posting.Category.ToLower() == category))
            .Where(posting => location == null ||
                (posting.Location != null && posting.Location.ToLower() == location))
            .ToListAsync(cancellationToken);

        var filtered = rows
            .Select(row => new
            {
                Row = row,
                MarketPosting = ToJobMarketPosting(row, options.Value.BusinessTimezone)
            })
            .Where(item => MatchesMemoryFilters(item.MarketPosting, query))
            .ToList();
        var factsWithSkills = filtered.Select(item => new PublicationPostingFact(
                item.Row.PostDateConfidence,
                item.Row.PublishedAt.HasValue
                    ? MarketPulseBusinessTime.GetBusinessDate(
                        item.Row.PublishedAt.Value,
                        options.Value.BusinessTimezone)
                    : null,
                item.Row.PostDateLowerBound.HasValue
                    ? DateOnly.FromDateTime(item.Row.PostDateLowerBound.Value)
                    : null,
                item.Row.PostDateUpperBound.HasValue
                    ? DateOnly.FromDateTime(item.Row.PostDateUpperBound.Value)
                    : null,
                overviewBuilder.ResolveSkillSlugs(
                    item.MarketPosting,
                    options.Value.TrackedKeywords)))
            .ToList();
        var selectedSkills = query.SkillSlugs
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Select(slug => slug.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return PublicationAnalyticsBuilder.Build(
            factsWithSkills,
            anchor,
            sourceDataAt,
            history is null ? null : DateOnly.FromDateTime(history.CoverageStart),
            history is null ? null : DateOnly.FromDateTime(history.CoverageEnd),
            query.Days,
            selectedSkills);
    }

    private static JobMarketPosting ToJobMarketPosting(
        JobPosting posting,
        string businessTimezone)
    {
        return new JobMarketPosting
        {
            Id = posting.ExternalId,
            SourceJobId = posting.SourceJobId,
            Source = "TopCV",
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
            PostedOn = posting.PublishedAt.HasValue
                ? MarketPulseBusinessTime.GetBusinessDate(posting.PublishedAt.Value, businessTimezone)
                : null,
            PostedOnText = posting.PostDateText,
            PostDateConfidence = posting.PostDateConfidence,
            PostDateLowerBound = posting.PostDateLowerBound.HasValue
                ? DateOnly.FromDateTime(posting.PostDateLowerBound.Value)
                : null,
            PostDateUpperBound = posting.PostDateUpperBound.HasValue
                ? DateOnly.FromDateTime(posting.PostDateUpperBound.Value)
                : null,
            PostDateObservedOn = posting.PostDateObservedOn.HasValue
                ? DateOnly.FromDateTime(posting.PostDateObservedOn.Value)
                : null,
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
            Days = query.Days is 7 or 14 or 30 or 90 ? query.Days : 30,
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
            !ContainsNormalized(posting.Title, query.Experience) &&
            !ContainsNormalized(
                JobMarketOverviewBuilder.ResolveExperienceLevel(posting),
                query.Experience))
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

    private static ScrapedJobPosting ToScrapedJobPosting(MarketPulseIngestPostingDto posting)
    {
        var title = TrimTo(posting.Title, 250) ?? "Untitled IT job";
        var url = TrimTo(posting.Url, 500) ?? string.Empty;
        var description = string.IsNullOrWhiteSpace(posting.Description)
            ? BuildIngestDescription(posting)
            : posting.Description.Trim();

        return new ScrapedJobPosting(
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
            CleanStringList(posting.Skills),
            PostDateConfidence: posting.PostDateConfidence,
            PostDateLowerBound: NormalizeDate(posting.PostDateLowerBound),
            PostDateUpperBound: NormalizeDate(posting.PostDateUpperBound),
            PostDateObservedOn: NormalizeDate(posting.PostDateObservedOn),
            IsActive: posting.IsActive,
            ExternalId: posting.Id);
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

            var fetchResult = await topCvClient.FetchImportBatchAsync(request, cancellationToken);
            if (fetchResult.Status is JobsApiFetchStatus.HttpError or
                JobsApiFetchStatus.Timeout or
                JobsApiFetchStatus.InvalidContract or
                JobsApiFetchStatus.StaleSource)
            {
                throw new MarketPulseSourceFetchException(fetchResult);
            }

            var lifecycleDecision = ResolveMissingLifecycleDecision(fetchResult);
            var status = !fetchResult.IsCompleteSync
                ? "partial"
                : fetchResult.Status == JobsApiFetchStatus.Empty
                    ? "empty"
                    : "success";
            var result = await PersistScrapedPostingsAsync(
                fetchResult.Postings,
                lifecycleDecision,
                run,
                fetchResult,
                status,
                null,
                cancellationToken);
            result.PostingsScraped = fetchResult.FetchedCount;
            result.SourceTotal = fetchResult.Total;
            result.SourceGeneratedAt = fetchResult.GeneratedAt;
            result.LatestSuccessfulCrawlAt = fetchResult.LatestSuccessfulCrawlAt;
            result.IsCompleteSync = fetchResult.IsCompleteSync;
            result.IsSourceFresh = fetchResult.IsSourceFresh;
            result.FetchStatus = fetchResult.Status.ToString();
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
                var runId = run.MarketPulseCrawlRunId;
                dbContext.ChangeTracker.Clear();
                var persistedRun = await dbContext.Set<MarketPulseCrawlRun>()
                    .SingleAsync(x => x.MarketPulseCrawlRunId == runId, cancellationToken);
                await RecordRefreshFailureAsync(persistedRun, ex, cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(request.SourceName) &&
            !string.Equals(request.SourceName.Trim(), DefaultSourceName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only TopCV ingest payloads are supported.", nameof(request));
        }
        var rawPostings = request.Postings
            .Select(ToScrapedJobPosting)
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        return await PersistScrapedPostingsAsync(
            rawPostings,
            MissingLifecycleDecision.Skip(LifecycleSkippedManualIngest),
            null,
            null,
            null,
            null,
            cancellationToken);
    }

    public async Task<MarketPulseRefreshResultDto> SyncPublicationHistoryAsync(
        MarketPulseHistorySyncRequestDto? request,
        CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request?.LookbackDays ?? options.Value.HistoryLookbackDays, 1, 730);
        var pageSize = Math.Clamp(
            request?.JobsApiPageSize ?? options.Value.JobsApiPageSize,
            1,
            500);
        var maxItems = Math.Clamp(
            request?.JobsApiMaxItems ?? 50_000,
            1,
            50_000);
        var maxPages = Math.Clamp(
            (int)Math.Ceiling((decimal)maxItems / pageSize),
            1,
            500);
        var fetchResult = await topCvClient.FetchImportBatchAsync(
            new MarketPulseRefreshRequestDto
            {
                JobsApiMaxItems = maxItems,
                JobsApiMaxPages = maxPages,
                JobsApiPageSize = pageSize
            },
            TopCvJobScope.All,
            cancellationToken);
        if (fetchResult.Status is not JobsApiFetchStatus.Success and not JobsApiFetchStatus.Empty)
        {
            throw new MarketPulseSourceFetchException(fetchResult);
        }
        if (!fetchResult.IsCompleteSync)
        {
            throw new InvalidOperationException(
                "TopCV history sync did not receive every scope=all page; publication coverage was not advanced.");
        }
        if (!fetchResult.IsSourceFresh)
        {
            throw new InvalidOperationException(
                "TopCV history sync requires fresh crawler metadata; publication coverage was not advanced.");
        }
        if (!fetchResult.LatestSuccessfulCrawlAt.HasValue)
        {
            throw new InvalidOperationException(
                "TopCV history sync requires latestSuccessfulCrawlAt metadata before advancing coverage.");
        }

        var result = await PersistScrapedPostingsAsync(
            fetchResult.Postings,
            MissingLifecycleDecision.Skip("historical_sync"),
            null,
            fetchResult,
            null,
            days,
            cancellationToken);
        InvalidateOverviewCache();
        result.Status = fetchResult.Status == JobsApiFetchStatus.Empty ? "empty" : "success";
        result.Mode = "history_sync";
        result.FetchStatus = fetchResult.Status.ToString();
        result.PostingsScraped = fetchResult.FetchedCount;
        result.SourceTotal = fetchResult.Total;
        result.SourceGeneratedAt = fetchResult.GeneratedAt;
        result.LatestSuccessfulCrawlAt = fetchResult.LatestSuccessfulCrawlAt;
        result.IsCompleteSync = fetchResult.IsCompleteSync;
        result.IsSourceFresh = fetchResult.IsSourceFresh;
        return result;
    }

    private async Task<MarketPulseRefreshResultDto> PersistScrapedPostingsAsync(
        IReadOnlyList<ScrapedJobPosting> rawPostings,
        MissingLifecycleDecision lifecycleDecision,
        MarketPulseCrawlRun? run,
        TopCvImportBatch? fetchResult,
        string? finalStatus,
        int? historyCoverageDays,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var settings = options.Value;
        var snapshotDate = fetchResult?.LatestSuccessfulCrawlAt is { } sourceObservedAt
            ? MarketPulseBusinessTime.GetBusinessDate(sourceObservedAt, settings.BusinessTimezone)
            : MarketPulseBusinessTime.GetBusinessDate(now, settings.BusinessTimezone);

        var savedPostings = 0;
        var newPostings = 0;
        var updatedPostings = 0;
        var seenPostings = 0;
        var duplicatePostings = 0;
        var expiredPostingsInRun = 0;
        var missingLifecycleApplied = false;
        var lifecycleSkippedReason = lifecycleDecision.SkippedReason;
        var lifecycleSkippedBelowMinimum = false;
        var lifecycleApplied = false;
        var missingThreshold = Math.Max(1, settings.MissingScansBeforeStale);
        var minimumLifecyclePostings = Math.Max(1, settings.MinimumPostingsForLifecycleCheck);
        var isHistoricalSync = historyCoverageDays.HasValue;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireImportAdvisoryLockAsync(cancellationToken);

        if (historyCoverageDays.HasValue && fetchResult is not null &&
            fetchResult.LatestSuccessfulCrawlAt.HasValue)
        {
            var sourceDataAt = fetchResult.LatestSuccessfulCrawlAt.Value.UtcDateTime;
            var latestImportedAt = await dbContext.Set<MarketPulseCrawlRun>()
                .AsNoTracking()
                .Where(item =>
                    (item.Status == "success" || item.Status == "empty") &&
                    item.SourceLatestSuccessAt.HasValue)
                .OrderByDescending(item => item.SourceLatestSuccessAt)
                .Select(item => item.SourceLatestSuccessAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (latestImportedAt.HasValue && NormalizeUtc(latestImportedAt.Value) > sourceDataAt)
            {
                throw new InvalidOperationException(
                    "TopCV history data is older than the latest successful import; refetch scope=all before syncing history.");
            }

            var historyState = await dbContext.Set<MarketPulsePublicationHistoryState>()
                .SingleOrDefaultAsync(item => item.SingletonId == 1, cancellationToken);
            if (historyState is not null && NormalizeUtc(historyState.SourceDataAt) > sourceDataAt)
            {
                throw new InvalidOperationException(
                    "TopCV history data is older than the existing publication-history watermark.");
            }

        }

        if (run is not null && fetchResult is not null && finalStatus is not null &&
            await IsSourceObservationAlreadyProcessedAsync(fetchResult, cancellationToken))
        {
            var skippedResult = new MarketPulseRefreshResultDto
            {
                SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
                SourcesScraped = 0,
                PostingsScraped = fetchResult.FetchedCount,
                ActivePostings = await dbContext.Set<JobPosting>()
                    .CountAsync(x => x.IsActive, cancellationToken),
                StalePostings = await dbContext.Set<JobPosting>()
                    .CountAsync(x => x.LifecycleStatus == LifecycleStaleUnverified, cancellationToken),
                ExpiredPostings = await dbContext.Set<JobPosting>()
                    .CountAsync(x => x.LifecycleStatus == LifecycleExpired, cancellationToken),
                MissingLifecycleApplied = false,
                LifecycleSkippedReason = LifecycleSkippedOutdatedObservation
            };
            await CompleteImportRunAsync(
                run,
                skippedResult,
                fetchResult,
                "skipped",
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return skippedResult;
        }

        if (rawPostings.Count > 0)
        {
            var sourcePostings = rawPostings;
            var uniquePostings = sourcePostings
                .GroupBy(BuildExternalId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            duplicatePostings += Math.Max(0, sourcePostings.Count - uniquePostings.Count);
            var lookupExternalIds = uniquePostings
                .SelectMany(x => new[]
                {
                    BuildExternalId(x),
                    BuildLegacyExternalId(x.Url),
                    x.SourceJobId?.Trim() ?? string.Empty
                })
                .Where(externalId => !string.IsNullOrWhiteSpace(externalId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingPostings = await dbContext.Set<JobPosting>()
                .Where(x => lookupExternalIds.Contains(x.ExternalId))
                .ToDictionaryAsync(x => x.ExternalId, cancellationToken);

            foreach (var rawPosting in uniquePostings)
            {
                var externalId = BuildExternalId(rawPosting);
                var publishedAt = NormalizeUtc(rawPosting.PublishedAt);
                var expiresAt = NormalizeUtc(rawPosting.ExpiresAt);
                var sourceUpdatedAt = NormalizeUtc(rawPosting.SourceUpdatedAt);
                var contentHash = BuildContentHash(rawPosting, expiresAt);
                var isExpired = !rawPosting.IsActive ||
                    (expiresAt.HasValue && DateOnly.FromDateTime(expiresAt.Value) < snapshotDate);

                if (!existingPostings.TryGetValue(externalId, out var posting))
                {
                    var legacyExternalId = BuildLegacyExternalId(rawPosting.Url);
                    existingPostings.TryGetValue(legacyExternalId, out posting);
                    if (posting is null && !string.IsNullOrWhiteSpace(rawPosting.SourceJobId))
                    {
                        existingPostings.TryGetValue(rawPosting.SourceJobId.Trim(), out posting);
                    }
                }

                if (posting is not null)
                {
                    var changed = !string.Equals(posting.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase);
                    var wasExpired = string.Equals(posting.LifecycleStatus, LifecycleExpired, StringComparison.OrdinalIgnoreCase);
                    var lifecycleChanged = posting.IsActive == isExpired ||
                        !string.Equals(
                            posting.LifecycleStatus,
                            isExpired ? LifecycleExpired : LifecycleActive,
                            StringComparison.OrdinalIgnoreCase);
                    var recordChanged = changed || lifecycleChanged;
                    var changeTimestamp = isHistoricalSync ? sourceUpdatedAt ?? now : now;

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
                    posting.Url = rawPosting.Url;
                    posting.Description = rawPosting.Description;
                    posting.PublishedAt = publishedAt;
                    posting.PostDateText = TrimTo(rawPosting.PostDateText, 80);
                    posting.PostDateConfidence = MarketPulseBusinessTime.NormalizePostDateConfidence(
                        rawPosting.PostDateConfidence);
                    posting.PostDateLowerBound = NormalizeDate(rawPosting.PostDateLowerBound);
                    posting.PostDateUpperBound = NormalizeDate(rawPosting.PostDateUpperBound);
                    posting.PostDateObservedOn = NormalizeDate(rawPosting.PostDateObservedOn);
                    posting.SourceUpdatedAt = sourceUpdatedAt;
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
                    if (!isHistoricalSync)
                    {
                        posting.MissingScanCount = 0;
                        posting.SeenCount++;
                        posting.LastSeenAt = now;
                        posting.LastCheckedAt = now;
                        posting.ScrapedAt = now;
                        posting.UpdatedAt = now;
                    }
                    else if (recordChanged)
                    {
                        posting.UpdatedAt = sourceUpdatedAt ?? now;
                    }
                    posting.LastChangedAt = recordChanged
                        ? changeTimestamp
                        : posting.LastChangedAt;
                    posting.ClosedDetectedAt = isExpired && posting.ClosedDetectedAt is null
                        ? changeTimestamp
                        : posting.ClosedDetectedAt;

                    if (recordChanged)
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
                    PostDateConfidence = MarketPulseBusinessTime.NormalizePostDateConfidence(
                        rawPosting.PostDateConfidence),
                    PostDateLowerBound = NormalizeDate(rawPosting.PostDateLowerBound),
                    PostDateUpperBound = NormalizeDate(rawPosting.PostDateUpperBound),
                    PostDateObservedOn = NormalizeDate(rawPosting.PostDateObservedOn),
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
                    lookupExternalIds,
                    snapshotDate,
                    now,
                    missingThreshold,
                    cancellationToken);
                lifecycleApplied = true;
            }
            else if (lifecycleDecision.ShouldApply)
            {
                lifecycleSkippedBelowMinimum = true;
            }
        }

        if (lifecycleDecision.ShouldApply && rawPostings.Count == 0)
        {
            expiredPostingsInRun += await MarkMissingPostingsAsync(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                snapshotDate,
                now,
                missingThreshold,
                cancellationToken);
            lifecycleApplied = true;
        }

        missingLifecycleApplied = lifecycleDecision.ShouldApply && lifecycleApplied;

        if (!missingLifecycleApplied && lifecycleSkippedBelowMinimum)
        {
            lifecycleSkippedReason = LifecycleSkippedBelowMinimum;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new MarketPulseRefreshResultDto
        {
            SnapshotDate = snapshotDate.ToDateTime(TimeOnly.MinValue),
            SourcesScraped = 1,
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

        if (run is not null && fetchResult is not null && finalStatus is not null)
        {
            await CompleteImportRunAsync(
                run,
                result,
                fetchResult,
                finalStatus,
                cancellationToken);
        }

        if (fetchResult is not null && historyCoverageDays.HasValue)
        {
            await UpdatePublicationHistoryStateAsync(
                fetchResult,
                historyCoverageDays.Value,
                now,
                cancellationToken);
        }
        else if (fetchResult is not null && run is not null)
        {
            await ExtendPublicationCoverageForNormalImportAsync(
                fetchResult,
                now,
                cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        InvalidateOverviewCache();

        return result;
    }

    private async Task UpdatePublicationHistoryStateAsync(
        TopCvImportBatch fetchResult,
        int historyCoverageDays,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!fetchResult.LatestSuccessfulCrawlAt.HasValue ||
            fetchResult.Status is not JobsApiFetchStatus.Success and not JobsApiFetchStatus.Empty ||
            !fetchResult.IsCompleteSync ||
            !fetchResult.IsSourceFresh)
        {
            return;
        }

        var state = await dbContext.Set<MarketPulsePublicationHistoryState>()
            .SingleOrDefaultAsync(item => item.SingletonId == 1, cancellationToken);
        var sourceDataAt = fetchResult.LatestSuccessfulCrawlAt.Value.UtcDateTime;
        var coverageEnd = MarketPulseBusinessTime.GetBusinessDate(
            fetchResult.LatestSuccessfulCrawlAt.Value,
            options.Value.BusinessTimezone);
        var requestedStart = coverageEnd.AddDays(-(historyCoverageDays - 1));
        var evidenceStart = fetchResult.HistoryCoverageStart.HasValue
            ? MarketPulseBusinessTime.GetBusinessDate(
                fetchResult.HistoryCoverageStart.Value,
                options.Value.BusinessTimezone)
            : coverageEnd;
        if (evidenceStart > coverageEnd)
        {
            evidenceStart = coverageEnd;
        }
        var effectiveStart = evidenceStart > requestedStart ? evidenceStart : requestedStart;
        if (state is null)
        {
            state = new MarketPulsePublicationHistoryState
            {
                SingletonId = 1,
                CoverageStart = effectiveStart.ToDateTime(TimeOnly.MinValue),
                CoverageEnd = coverageEnd.ToDateTime(TimeOnly.MinValue),
                SourceDataAt = sourceDataAt,
                LastSuccessfulSyncAt = now,
                SyncedPostingCount = fetchResult.FetchedCount,
                UpdatedAt = now
            };
            dbContext.Set<MarketPulsePublicationHistoryState>().Add(state);
            return;
        }

        var effectiveStartDateTime = effectiveStart.ToDateTime(TimeOnly.MinValue);
        if (effectiveStartDateTime < state.CoverageStart)
        {
            state.CoverageStart = effectiveStartDateTime;
        }
        state.LastSuccessfulSyncAt = now;
        state.SyncedPostingCount = fetchResult.FetchedCount;

        var coverageEndDateTime = coverageEnd.ToDateTime(TimeOnly.MinValue);
        if (coverageEndDateTime > state.CoverageEnd)
        {
            state.CoverageEnd = coverageEndDateTime;
        }
        if (sourceDataAt > state.SourceDataAt)
        {
            state.SourceDataAt = sourceDataAt;
        }
        state.UpdatedAt = now;
    }

    private async Task ExtendPublicationCoverageForNormalImportAsync(
        TopCvImportBatch fetchResult,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!fetchResult.LatestSuccessfulCrawlAt.HasValue ||
            fetchResult.Status is not JobsApiFetchStatus.Success and not JobsApiFetchStatus.Empty ||
            !fetchResult.IsCompleteSync ||
            !fetchResult.IsSourceFresh)
        {
            return;
        }

        var state = await dbContext.Set<MarketPulsePublicationHistoryState>()
            .SingleOrDefaultAsync(item => item.SingletonId == 1, cancellationToken);
        if (state is null)
        {
            return;
        }

        var sourceDate = MarketPulseBusinessTime.GetBusinessDate(
            fetchResult.LatestSuccessfulCrawlAt.Value,
            options.Value.BusinessTimezone);
        var currentCoverageEnd = DateOnly.FromDateTime(state.CoverageEnd);
        if (sourceDate > currentCoverageEnd.AddDays(1))
        {
            return;
        }

        if (sourceDate > currentCoverageEnd)
        {
            state.CoverageEnd = sourceDate.ToDateTime(TimeOnly.MinValue);
        }
        state.UpdatedAt = now;
    }

    private async Task AcquireImportAdvisoryLockAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(
                dbContext.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            return;
        }

        // The transaction-scoped database lock serializes API and cron imports across processes.
        // Freshness is checked only after this lock is held, so same-day newer-wins remains atomic.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({ImportAdvisoryLockKey})",
            cancellationToken);
    }

    private async Task<bool> IsSourceObservationAlreadyProcessedAsync(
        TopCvImportBatch fetchResult,
        CancellationToken cancellationToken)
    {
        if (!fetchResult.LatestSuccessfulCrawlAt.HasValue)
        {
            return false;
        }

        var latestCompletedRunAt = await dbContext.Set<MarketPulseCrawlRun>()
            .AsNoTracking()
            .Where(x =>
                (x.Status == "success" || x.Status == "empty") &&
                x.IsCompleteSync &&
                x.SourceLatestSuccessAt.HasValue)
            .OrderByDescending(x => x.SourceLatestSuccessAt)
            .Select(x => x.SourceLatestSuccessAt)
            .FirstOrDefaultAsync(cancellationToken);
        var latestKnownAt = latestCompletedRunAt.HasValue
            ? NormalizeUtc(latestCompletedRunAt.Value)
            : DateTime.MinValue;
        return fetchResult.LatestSuccessfulCrawlAt.Value.UtcDateTime <= latestKnownAt;
    }

    private MissingLifecycleDecision ResolveMissingLifecycleDecision(TopCvImportBatch fetchResult)
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

    private static Task CompleteImportRunAsync(
        MarketPulseCrawlRun run,
        MarketPulseRefreshResultDto result,
        TopCvImportBatch fetchResult,
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

        return Task.CompletedTask;
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
            Stage = "import",
            ErrorCode = fetchException?.FetchResult.Status.ToString() ?? exception.GetType().Name,
            ErrorMessage = errorSummary ?? "Market Pulse refresh failed.",
            ErrorDetail = TrimTo(exception.ToString(), 4000),
            RetryCount = 0,
            Status = "open",
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> MarkMissingPostingsAsync(
        IReadOnlySet<string> observedExternalIds,
        DateOnly snapshotDate,
        DateTime now,
        int missingThreshold,
        CancellationToken cancellationToken)
    {
        var expiredInRun = 0;
        var businessTimezone = options.Value.BusinessTimezone;
        var missingPostings = await dbContext.Set<JobPosting>()
            .Where(x =>
                x.LifecycleStatus != LifecycleExpired &&
                !observedExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken);

        foreach (var posting in missingPostings)
        {
            if (MarketPulseBusinessTime.GetBusinessDate(posting.LastCheckedAt, businessTimezone) < snapshotDate)
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

    private static string BuildExternalId(ScrapedJobPosting posting)
    {
        if (!string.IsNullOrWhiteSpace(posting.ExternalId))
        {
            var externalId = posting.ExternalId.Trim();
            if (externalId.StartsWith(DefaultSourceName + ":", StringComparison.OrdinalIgnoreCase))
            {
                return TrimTo(externalId, 120)!;
            }
        }

        if (!string.IsNullOrWhiteSpace(posting.SourceJobId))
        {
            var sourceJobId = posting.SourceJobId.Trim();
            if (sourceJobId.StartsWith(DefaultSourceName + ":", StringComparison.OrdinalIgnoreCase))
            {
                return TrimTo(sourceJobId, 120)!;
            }
            var prefixed = $"{DefaultSourceName}:{sourceJobId}";
            return prefixed.Length <= 120
                ? prefixed
                : $"{DefaultSourceName}:{HashIdentity($"source_job_id:{sourceJobId}")}";
        }

        var normalizedUrl = NormalizeUrlForIdentity(posting.Url);
        return $"{DefaultSourceName}:{HashIdentity($"url:{normalizedUrl}")}";
    }

    private static string BuildLegacyExternalId(string url)
    {
        return HashIdentity($"{DefaultSourceName}:{url.Trim()}");
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
            NormalizeForHash(posting.PostDateLowerBound?.ToString("O")),
            NormalizeForHash(posting.PostDateUpperBound?.ToString("O")),
            NormalizeForHash(posting.PostDateObservedOn?.ToString("O")),
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

    private static DateTime? NormalizeDate(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Unspecified)
            : null;
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

}

internal sealed record SalaryRange(decimal? MinMonthlyVnd, decimal? MaxMonthlyVnd);

internal sealed class MarketPulseSourceFetchException(TopCvImportBatch fetchResult)
    : Exception(fetchResult.ErrorMessage ?? $"Jobs API fetch failed with status {fetchResult.Status}.")
{
    public TopCvImportBatch FetchResult { get; } = fetchResult;
}
