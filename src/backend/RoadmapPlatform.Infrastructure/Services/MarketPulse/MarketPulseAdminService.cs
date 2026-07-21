using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseAdminService(
    ApplicationDbContext dbContext,
    IMarketPulseService marketPulseService,
    IJobsApiHealthService jobsApiHealthService,
    TopCvJobsApiClient topCvClient) : IMarketPulseAdminService
{
    private static readonly IReadOnlyList<string> DefaultCategories =
    [
        "Backend",
        "Frontend",
        "Fullstack",
        "Mobile",
        "DevOps",
        "Data",
        "AI/ML",
        "QA/Testing",
        "Security",
        "UI/UX",
        "Project/Product Management",
        "Other"
    ];

    public async Task<MarketPulseAdminDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var overviewTask = marketPulseService.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Days = 7 },
            cancellationToken);
        var healthTask = jobsApiHealthService.GetHealthAsync(cancellationToken);
        await Task.WhenAll(overviewTask, healthTask);
        var overview = await overviewTask;
        var health = await healthTask;
        var crawlerCritical = IsCriticalCrawlerHealth(health);
        var crawlerDegraded = crawlerCritical || IsDegradedCrawlerHealth(health);
        int openCrawlerFailures;
        try
        {
            openCrawlerFailures = await topCvClient.GetOpenCrawlerFailureCountAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            openCrawlerFailures = health.PagesFailed + health.PagesBlocked;
        }
        var latestRun = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(run => run.OperationType == "import")
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var latestSuccessfulImport = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(run =>
                run.OperationType == "import" &&
                (run.Status == "success" || run.Status == "empty") &&
                run.IsCompleteSync)
            .OrderByDescending(run => run.FinishedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var openFailures = await dbContext.Set<MarketPulseFailedItem>()
            .AsNoTracking()
            .CountAsync(
                item =>
                    item.Status == "open" ||
                    item.Status == "retry_queued" ||
                    item.Status == "retrying",
                cancellationToken);
        var operations = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(operation => operation.OperationType == "refresh")
            .OrderByDescending(operation => operation.RequestedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        var latestSuccessfulRefreshAt = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(operation => operation.OperationType == "refresh" && operation.Status == "success")
            .OrderByDescending(operation => operation.FinishedAt)
            .Select(operation => operation.FinishedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var analytics = overview.PublicationAnalytics;
        decimal? importLag = latestSuccessfulImport?.SourceLatestSuccessAt.HasValue == true &&
            latestSuccessfulImport.FinishedAt.HasValue
            ? Math.Max(
                0,
                (decimal)(latestSuccessfulImport.FinishedAt.Value -
                    latestSuccessfulImport.SourceLatestSuccessAt.Value).TotalMinutes)
            : null;
        var alerts = new List<MarketPulseAdminAlertDto>();
        if (crawlerCritical)
        {
            alerts.Add(new MarketPulseAdminAlertDto
            {
                Severity = "critical",
                Code = "TOPCV_CRAWLER_UNAVAILABLE",
                Message = health.ErrorMessage ?? "TopCV crawler is unavailable.",
                Action = "Check Python crawler logs"
            });
        }
        else if (crawlerDegraded)
        {
            alerts.Add(new MarketPulseAdminAlertDto
            {
                Severity = "warning",
                Code = "TOPCV_CRAWLER_DEGRADED",
                Message = health.ErrorMessage ??
                    $"TopCV crawler health is {health.Status}; latest listing status is {health.LatestListingStatus ?? "unknown"}.",
                Action = "Review crawler runs and failures"
            });
        }
        if (latestRun?.Status is "failed" or "stale_source" or "partial")
        {
            alerts.Add(new MarketPulseAdminAlertDto
            {
                Severity = latestRun.Status is "failed" or "stale_source" ? "critical" : "warning",
                Code = "TOPCV_IMPORT_INCOMPLETE",
                Message = latestRun.ErrorSummary ??
                    $"The latest .NET TopCV import ended with status '{latestRun.Status}'.",
                Action = "Review import run details"
            });
        }
        if (analytics.Availability != "available")
        {
            alerts.Add(new MarketPulseAdminAlertDto
            {
                Severity = "warning",
                Code = "PUBLICATION_HISTORY_INCOMPLETE",
                Message = "Publication history does not fully cover both comparison periods.",
                Action = "Run historical sync"
            });
        }
        if (analytics.PostDateQuality.ReliablePercent < 50)
        {
            alerts.Add(new MarketPulseAdminAlertDto
            {
                Severity = "warning",
                Code = "POST_DATE_QUALITY_LOW",
                Message = "Fewer than half of TopCV postings have reliable publication dates.",
                Action = "Run Python post-date backfill"
            });
        }

        var overall = alerts.Any(alert => alert.Severity == "critical")
            ? "critical"
            : alerts.Count > 0 || openFailures > 0 || openCrawlerFailures > 0
                ? "degraded"
                : "healthy";
        return new MarketPulseAdminDashboardDto
        {
            OverallStatus = overall,
            LatestSuccessfulRefreshAt = latestSuccessfulRefreshAt,
            CurrentOperation = operations
                .Where(operation =>
                    operation.Status == "queued" ||
                    operation.Status == "crawling" ||
                    operation.Status == "importing")
                .Select(ToOperationDto)
                .FirstOrDefault(),
            ActiveJobs = overview.ActivePostings,
            EstimatedPostings7Days = analytics.CurrentPeriod.EstimatedTotal,
            CrawlerFreshnessHours = health.HoursSinceSuccessfulCrawl.HasValue
                ? Math.Round((decimal)health.HoursSinceSuccessfulCrawl.Value, 1)
                : null,
            ReliablePostDateCoverage = analytics.PostDateQuality.ReliablePercent,
            AnalyticsConfidence = analytics.Confidence,
            PostDateQuality = analytics.PostDateQuality,
            ImportLagMinutes = importLag.HasValue ? Math.Round(importLag.Value, 1) : null,
            OpenCrawlerFailures = openCrawlerFailures,
            OpenImportFailures = openFailures,
            PipelineHealth =
            [
                new MarketPulsePipelineHealthItemDto
                {
                    Key = "crawler",
                    Label = "Python TopCV crawler",
                    Status = crawlerCritical
                        ? "critical"
                        : crawlerDegraded
                            ? "degraded"
                            : "healthy",
                    Detail = health.LatestSuccessfulCrawlAt.HasValue
                        ? $"Latest success {health.LatestSuccessfulCrawlAt:O}"
                        : "No successful crawl metadata"
                },
                new MarketPulsePipelineHealthItemDto
                {
                    Key = "import",
                    Label = ".NET TopCV import",
                    Status = latestRun?.Status ?? "unknown",
                    Detail = latestRun?.FinishedAt.HasValue == true
                        ? $"Latest run finished {latestRun.FinishedAt:O}"
                        : "No completed import"
                },
                new MarketPulsePipelineHealthItemDto
                {
                    Key = "history",
                    Label = "Publication-history coverage",
                    Status = analytics.Availability == "available" ? "healthy" : "degraded",
                    Detail = analytics.HistoryCoverageStart.HasValue
                        ? $"{analytics.HistoryCoverageStart:yyyy-MM-dd} to {analytics.HistoryCoverageEnd:yyyy-MM-dd}"
                        : "Historical sync required"
                },
                new MarketPulsePipelineHealthItemDto
                {
                    Key = "quality",
                    Label = "Detail/category/salary quality",
                    Status = overview.DataQuality.Level,
                    Detail = $"Detail {overview.DataQuality.DetailCoveragePercent:F1}%, salary {overview.DataQuality.SalaryCoveragePercent:F1}%"
                }
            ],
            Alerts = alerts,
            DemandTrend = analytics.MarketTrendPoints,
            RecentOperations = operations.Select(ToOperationDto).ToList()
        };
    }

    public async Task<MarketPulseRefreshOperationDto> CreateRefreshOperationAsync(
        CancellationToken cancellationToken)
    {
        var active = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .OrderByDescending(operation => operation.RequestedAt)
            .FirstOrDefaultAsync(operation =>
                operation.OperationType == "refresh" &&
                (operation.Status == "queued" ||
                 operation.Status == "crawling" ||
                 operation.Status == "importing"),
                cancellationToken);
        if (active is not null)
        {
            throw new MarketPulseRefreshOperationConflictException(ToOperationDto(active));
        }

        var baseline = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(run => run.OperationType == "import" && run.SourceLatestSuccessAt.HasValue)
            .MaxAsync(run => (DateTime?)run.SourceLatestSuccessAt, cancellationToken) ?? DateTime.UnixEpoch;
        var crawlerHealth = await jobsApiHealthService.GetHealthAsync(cancellationToken);
        if (crawlerHealth.LatestSuccessfulCrawlAt.HasValue)
        {
            var crawlerBaseline = NormalizeUtc(crawlerHealth.LatestSuccessfulCrawlAt.Value);
            if (crawlerBaseline > baseline)
            {
                baseline = crawlerBaseline;
            }
        }
        var now = DateTime.UtcNow;
        var operation = new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "refresh",
            Status = "queued",
            Mode = "end_to_end",
            CurrentStep = "crawler",
            BaselineCrawlerSuccessAt = baseline,
            TriggerType = "manual",
            RequestedAt = now,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Set<MarketPulsePipelineRun>().Add(operation);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var concurrent = await dbContext.Set<MarketPulsePipelineRun>()
                .AsNoTracking()
                .OrderByDescending(item => item.RequestedAt)
                .FirstOrDefaultAsync(item =>
                    item.OperationType == "refresh" &&
                    (item.Status == "queued" ||
                     item.Status == "crawling" ||
                     item.Status == "importing"),
                    cancellationToken);
            if (concurrent is not null)
            {
                throw new MarketPulseRefreshOperationConflictException(ToOperationDto(concurrent));
            }
            throw;
        }
        return ToOperationDto(operation);
    }

    public async Task<MarketPulseRefreshOperationDto?> GetCurrentRefreshOperationAsync(
        CancellationToken cancellationToken)
    {
        var operation = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .OrderByDescending(item => item.RequestedAt)
            .FirstOrDefaultAsync(item =>
                item.OperationType == "refresh" &&
                (item.Status == "queued" || item.Status == "crawling" || item.Status == "importing"),
                cancellationToken);
        return operation is null ? null : ToOperationDto(operation);
    }

    public async Task<MarketPulseRefreshOperationDto?> GetRefreshOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var operation = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.OperationType == "refresh" && item.MarketPulsePipelineRunId == operationId,
                cancellationToken);
        return operation is null ? null : ToOperationDto(operation);
    }

    public async Task<MarketPulseFailureGroupsDto> GetFailureGroupsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MarketPulseCrawlerFailureDto> crawlerFailures;
        try
        {
            crawlerFailures = await topCvClient.GetCrawlerFailuresAsync(
                query.Status,
                query.Limit,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var health = await jobsApiHealthService.GetHealthAsync(cancellationToken);
            crawlerFailures = health.PagesFailed + health.PagesBlocked <= 0 && !health.IsBlocked
                ? []
                :
                [
                    new MarketPulseCrawlerFailureDto
                    {
                        FailureId = "crawler-health",
                        Stage = "listing",
                        ErrorCode = health.IsBlocked ? "CRAWLER_BLOCKED" : "CRAWLER_PAGE_FAILURE",
                        ErrorMessage = health.ErrorMessage ??
                            $"TopCV crawler reports {health.PagesFailed} failed and {health.PagesBlocked} blocked pages.",
                        Status = "open",
                        CreatedAt = health.LatestListingFinishedAt,
                        Actionable = false
                    }
                ];
        }
        return new MarketPulseFailureGroupsDto
        {
            CrawlerFailures = crawlerFailures,
            ImportFailures = await GetFailedItemsAsync(query, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<MarketPulseCrawlRunDto>> GetCrawlRunsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, 200);
        var runs = dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(run => run.OperationType == "import");

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            runs = runs.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            ValidateTopCvSource(query.Source);
        }

        if (query.From.HasValue)
        {
            runs = runs.Where(x => x.StartedAt >= NormalizeUtc(query.From.Value));
        }

        if (query.To.HasValue)
        {
            runs = runs.Where(x => x.StartedAt <= NormalizeUtc(query.To.Value));
        }

        var rows = await runs
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToRunDto).ToList();
    }

    public async Task<IReadOnlyList<MarketPulseFailedItemDto>> GetFailedItemsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, 200);
        var items = dbContext.Set<MarketPulseFailedItem>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            items = items.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            ValidateTopCvSource(query.Source);
        }

        if (query.From.HasValue)
        {
            items = items.Where(x => x.CreatedAt >= NormalizeUtc(query.From.Value));
        }

        if (query.To.HasValue)
        {
            items = items.Where(x => x.CreatedAt <= NormalizeUtc(query.To.Value));
        }

        var rows = await items
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToFailedItemDto).ToList();
    }

    public async Task<IReadOnlyList<MarketPulseFailedItemDto>> RetryFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken)
    {
        var ids = failedItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var selected = await dbContext.Set<MarketPulseFailedItem>()
            .Where(item =>
                ids.Contains(item.MarketPulseFailedItemId) &&
                (item.Status == "open" || item.Status == "retry_queued"))
            .ToListAsync(cancellationToken);
        if (selected.Count == 0)
        {
            return [];
        }
        var selectedIds = selected
            .Select(item => item.MarketPulseFailedItemId)
            .ToList();

        foreach (var item in selected)
        {
            item.Status = "retrying";
            item.RetryCount++;
            item.LastRetryAt = now;
            item.UpdatedAt = now;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await marketPulseService.RefreshAsync(cancellationToken);
            var replayedSuccessfulObservation = result.Status == "skipped" &&
                result.LifecycleSkippedReason == "source_observation_not_newer";
            var succeeded = (result.Status is "success" or "empty" || replayedSuccessfulObservation) &&
                result.IsSourceFresh &&
                result.IsCompleteSync;
            selected = await ReloadFailedItemsAsync(selectedIds, cancellationToken);
            foreach (var item in selected)
            {
                item.Status = succeeded ? "resolved" : "open";
                item.UpdatedAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            return selected.Select(ToFailedItemDto).ToList();
        }
        catch
        {
            selected = await ReloadFailedItemsAsync(selectedIds, CancellationToken.None);
            foreach (var item in selected)
            {
                item.Status = "open";
                item.UpdatedAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public Task<IReadOnlyList<MarketPulseFailedItemDto>> IgnoreFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken) =>
        UpdateFailedItemStatusAsync(failedItemIds, "ignored", incrementRetry: false, cancellationToken);

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var configured = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .Select(x => x.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        return DefaultCategories
            .Concat(configured)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x == "Other" ? "zzzz" : x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<MarketPulseClassifierMappingDto>> GetClassifierMappingsAsync(
        CancellationToken cancellationToken)
    {
        await EnsureDefaultClassifierMappingsAsync(cancellationToken);

        var rows = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Keyword)
            .ToListAsync(cancellationToken);

        return rows.Select(ToMappingDto).ToList();
    }

    public async Task<MarketPulseClassifierMappingDto> CreateClassifierMappingAsync(
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeMappingRequest(request);
        var now = DateTime.UtcNow;
        var exists = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(x => x.Keyword == normalized.Keyword && x.Category == normalized.Category, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("This classifier keyword mapping already exists.");
        }

        var mapping = new MarketPulseClassifierKeywordMapping
        {
            MarketPulseClassifierKeywordMappingId = Guid.NewGuid(),
            Keyword = normalized.Keyword,
            Category = normalized.Category,
            IsEnabled = normalized.IsEnabled,
            Weight = normalized.Weight,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<MarketPulseClassifierKeywordMapping>().Add(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToMappingDto(mapping);
    }

    public async Task<MarketPulseClassifierMappingDto> UpdateClassifierMappingAsync(
        Guid mappingId,
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .FirstOrDefaultAsync(x => x.MarketPulseClassifierKeywordMappingId == mappingId, cancellationToken);

        if (mapping is null)
        {
            throw new KeyNotFoundException("Classifier mapping was not found.");
        }

        var normalized = NormalizeMappingRequest(request);
        var duplicate = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(x =>
                x.MarketPulseClassifierKeywordMappingId != mappingId &&
                x.Keyword == normalized.Keyword &&
                x.Category == normalized.Category,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Another classifier mapping already uses this keyword and category.");
        }

        mapping.Keyword = normalized.Keyword;
        mapping.Category = normalized.Category;
        mapping.IsEnabled = normalized.IsEnabled;
        mapping.Weight = normalized.Weight;
        mapping.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMappingDto(mapping);
    }

    public async Task DeleteClassifierMappingAsync(Guid mappingId, CancellationToken cancellationToken)
    {
        var mapping = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .FirstOrDefaultAsync(x => x.MarketPulseClassifierKeywordMappingId == mappingId, cancellationToken);

        if (mapping is null)
        {
            return;
        }

        dbContext.Set<MarketPulseClassifierKeywordMapping>().Remove(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketPulseClassifierTestResultDto> TestClassifierAsync(
        MarketPulseClassifierTestRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultClassifierMappingsAsync(cancellationToken);

        var text = NormalizeText(request.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new MarketPulseClassifierTestResultDto
            {
                Category = "Other",
                Confidence = 0,
                FallbackCategory = "Other",
                Matches = []
            };
        }

        var mappings = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        var matches = mappings
            .Where(x => ContainsKeyword(text, x.Keyword))
            .Select(x => new MarketPulseClassifierMatchDto
            {
                Keyword = x.Keyword,
                Category = x.Category,
                Weight = x.Weight
            })
            .ToList();

        if (matches.Count == 0)
        {
            return new MarketPulseClassifierTestResultDto
            {
                Category = "Other",
                Confidence = 0,
                FallbackCategory = "Other",
                Matches = []
            };
        }

        var totalWeight = matches.Sum(x => x.Weight);
        var best = matches
            .GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .Select(x => new
            {
                Category = x.Key,
                Weight = x.Sum(item => item.Weight),
                Count = x.Count()
            })
            .OrderByDescending(x => x.Weight)
            .ThenByDescending(x => x.Count)
            .ThenBy(x => x.Category)
            .First();

        return new MarketPulseClassifierTestResultDto
        {
            Category = best.Category,
            Confidence = totalWeight <= 0 ? 0 : Math.Round(best.Weight / totalWeight, 2),
            FallbackCategory = "Other",
            Matches = matches
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.Keyword)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<MarketPulseSourceHealthDto>> GetSourceHealthAsync(
        CancellationToken cancellationToken)
    {
        var latest = await dbContext.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .Where(run => run.OperationType == "import")
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null)
        {
            return [];
        }
        return
        [
            new MarketPulseSourceHealthDto
            {
                Source = "topcv",
                Status = latest.Status,
                LastSuccessAt = latest.Status is "success" or "empty" ? latest.FinishedAt : null,
                LastFailureAt = latest.Status is "failed" or "stale_source" ? latest.FinishedAt : null,
                SourceGeneratedAt = latest.SourceGeneratedAt,
                SourceLatestSuccessAt = latest.SourceLatestSuccessAt,
                LastRunId = latest.MarketPulsePipelineRunId,
                LastErrorSummary = latest.ErrorSummary,
                UpdatedAt = latest.FinishedAt ?? latest.StartedAt
            }
        ];
    }

    private async Task<IReadOnlyList<MarketPulseFailedItemDto>> UpdateFailedItemStatusAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        string status,
        bool incrementRetry,
        CancellationToken cancellationToken)
    {
        var ids = failedItemIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var items = await dbContext.Set<MarketPulseFailedItem>()
            .Where(x => ids.Contains(x.MarketPulseFailedItemId))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.Status = status;
            item.UpdatedAt = now;
            if (incrementRetry)
            {
                item.RetryCount++;
                item.LastRetryAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return items.Select(ToFailedItemDto).ToList();
    }

    private async Task<List<MarketPulseFailedItem>> ReloadFailedItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        foreach (var tracked in dbContext.ChangeTracker.Entries<MarketPulseFailedItem>().ToList())
        {
            tracked.State = EntityState.Detached;
        }
        return await dbContext.Set<MarketPulseFailedItem>()
            .Where(item =>
                ids.Contains(item.MarketPulseFailedItemId) &&
                item.Status == "retrying")
            .ToListAsync(cancellationToken);
    }

    private static bool IsCriticalCrawlerHealth(MarketPulseExternalSourceHealthDto health)
    {
        if (!health.IsAvailable || health.IsBlocked)
        {
            return true;
        }

        var healthStatus = health.Status.Trim().ToLowerInvariant();
        var listingStatus = health.LatestListingStatus?.Trim().ToLowerInvariant();
        return healthStatus is
                "critical" or
                "failed" or
                "blocked" or
                "layout_changed" or
                "unavailable" or
                "unauthorized" or
                "invalid_contract" or
                "http_error" or
                "timeout" ||
            listingStatus is "failed" or "blocked" or "layout_changed";
    }

    private static bool IsDegradedCrawlerHealth(MarketPulseExternalSourceHealthDto health)
    {
        var healthStatus = health.Status.Trim().ToLowerInvariant();
        var listingStatus = health.LatestListingStatus?.Trim().ToLowerInvariant();
        return health.IsStale ||
            healthStatus is "stale" or "warning" or "degraded" or "rate_limited" ||
            listingStatus is "partial" or "partial_success" or "empty_protected";
    }

    private async Task EnsureDefaultClassifierMappingsAsync(CancellationToken cancellationToken)
    {
        var hasMappings = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(cancellationToken);
        if (hasMappings)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var defaults = new[]
        {
            ("backend", "Backend"),
            ("java", "Backend"),
            ("spring", "Backend"),
            ("asp.net", "Backend"),
            ("frontend", "Frontend"),
            ("react", "Frontend"),
            ("vue", "Frontend"),
            ("angular", "Frontend"),
            ("fullstack", "Fullstack"),
            ("full stack", "Fullstack"),
            ("mobile", "Mobile"),
            ("android", "Mobile"),
            ("ios", "Mobile"),
            ("devops", "DevOps"),
            ("kubernetes", "DevOps"),
            ("data engineer", "Data"),
            ("data analyst", "Data"),
            ("machine learning", "AI/ML"),
            ("ai engineer", "AI/ML"),
            ("qa", "QA/Testing"),
            ("tester", "QA/Testing"),
            ("security", "Security"),
            ("ui ux", "UI/UX"),
            ("product manager", "Project/Product Management"),
            ("project manager", "Project/Product Management")
        };

        dbContext.Set<MarketPulseClassifierKeywordMapping>().AddRange(defaults.Select(item =>
            new MarketPulseClassifierKeywordMapping
            {
                MarketPulseClassifierKeywordMappingId = Guid.NewGuid(),
                Keyword = item.Item1,
                Category = item.Item2,
                IsEnabled = true,
                Weight = 1,
                CreatedAt = now,
                UpdatedAt = now
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MarketPulseClassifierMappingRequestDto NormalizeMappingRequest(
        MarketPulseClassifierMappingRequestDto request)
    {
        var keyword = NormalizeWhitespace(request.Keyword).ToLowerInvariant();
        var category = NormalizeWhitespace(request.Category);
        var weight = Math.Round(Math.Clamp(request.Weight, 0.1m, 100m), 2);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Keyword is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.");
        }

        return new MarketPulseClassifierMappingRequestDto
        {
            Keyword = keyword,
            Category = category,
            IsEnabled = request.IsEnabled,
            Weight = weight
        };
    }

    private static MarketPulseCrawlRunDto ToRunDto(MarketPulsePipelineRun run) => new()
    {
        RunId = run.MarketPulsePipelineRunId,
        Source = "topcv",
        Status = run.Status,
        Mode = run.Mode,
        TriggerType = run.TriggerType,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        DurationMs = run.DurationMs,
        FetchedCount = run.FetchedCount,
        SourceTotalCount = run.SourceTotalCount,
        IsCompleteSync = run.IsCompleteSync,
        MissingLifecycleApplied = run.MissingLifecycleApplied,
        LifecycleSkippedReason = run.LifecycleSkippedReason,
        SourceGeneratedAt = run.SourceGeneratedAt,
        SourceLatestSuccessAt = run.SourceLatestSuccessAt,
        SavedCount = run.SavedCount,
        ImportedCount = run.ImportedCount,
        UpdatedCount = run.UpdatedCount,
        SkippedCount = run.SkippedCount,
        DuplicateCount = run.DuplicateCount,
        FailedCount = run.FailedCount,
        StoppedReason = run.StoppedReason,
        ErrorSummary = run.ErrorSummary
    };

    private static MarketPulseFailedItemDto ToFailedItemDto(MarketPulseFailedItem item) => new()
    {
        FailedItemId = item.MarketPulseFailedItemId,
        RunId = item.MarketPulsePipelineRunId,
        Source = "topcv",
        Url = item.Url,
        Stage = item.Stage,
        ErrorCode = item.ErrorCode,
        ErrorMessage = item.ErrorMessage,
        RetryCount = item.RetryCount,
        CreatedAt = item.CreatedAt,
        LastRetryAt = item.LastRetryAt,
        Status = item.Status,
        ErrorDetail = item.ErrorDetail
    };

    private static MarketPulseClassifierMappingDto ToMappingDto(
        MarketPulseClassifierKeywordMapping mapping) => new()
    {
        MappingId = mapping.MarketPulseClassifierKeywordMappingId,
        Keyword = mapping.Keyword,
        Category = mapping.Category,
        IsEnabled = mapping.IsEnabled,
        Weight = mapping.Weight,
        CreatedAt = mapping.CreatedAt,
        UpdatedAt = mapping.UpdatedAt
    };

    private static MarketPulseRefreshOperationDto ToOperationDto(
        MarketPulsePipelineRun operation) => new()
    {
        OperationId = operation.MarketPulsePipelineRunId,
        Status = operation.Status,
        CurrentStep = operation.CurrentStep ?? "crawler",
        BaselineCrawlerSuccessAt = operation.BaselineCrawlerSuccessAt,
        CrawlerSuccessAt = operation.CrawlerSuccessAt,
        ImportRunId = operation.ImportRunId,
        ErrorCode = operation.ErrorCode,
        ErrorMessage = operation.ErrorMessage,
        RequestedAt = operation.RequestedAt,
        StartedAt = operation.StartedAt,
        FinishedAt = operation.FinishedAt,
        UpdatedAt = operation.UpdatedAt
    };

    private static void ValidateTopCvSource(string source)
    {
        if (!string.Equals(source.Trim(), "topcv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Market Pulse only supports source='topcv'.", nameof(source));
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static bool ContainsKeyword(string normalizedText, string keyword)
    {
        var normalizedKeyword = Regex.Escape(NormalizeText(keyword));
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return false;
        }

        return Regex.IsMatch(normalizedText, $@"(^|[^a-z0-9]){normalizedKeyword}([^a-z0-9]|$)");
    }

    private static string NormalizeText(string? value) =>
        RemoveDiacritics(NormalizeWhitespace(value).ToLowerInvariant());

    private static string NormalizeWhitespace(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string RemoveDiacritics(string value)
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
}

public sealed class MarketPulseRefreshOperationConflictException(
    MarketPulseRefreshOperationDto currentOperation)
    : InvalidOperationException("A TopCV refresh operation is already active.")
{
    public MarketPulseRefreshOperationDto CurrentOperation { get; } = currentOperation;
}
