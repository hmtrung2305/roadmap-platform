using System.Net;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests.MarketPulse;

public sealed class MarketPulseAdminTests
{
    [Fact]
    public async Task TC241_CreateRefreshOperation_WhenNoActiveOperation_ShouldPersistOneQueuedOperation()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();

        var created = await fixture.AdminService.CreateRefreshOperationAsync(CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var reloaded = await fixture.AdminService.GetRefreshOperationAsync(
            created.OperationId,
            CancellationToken.None);
        var current = await fixture.AdminService.GetCurrentRefreshOperationAsync(CancellationToken.None);
        var persistedCount = await fixture.Context.Set<MarketPulsePipelineRun>()
            .CountAsync(run => run.OperationType == "refresh");

        Assert.Equal("queued", created.Status);
        Assert.Equal("crawler", created.CurrentStep);
        Assert.Equal(created.OperationId, reloaded?.OperationId);
        Assert.Equal(created.OperationId, current?.OperationId);
        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public async Task TC242_Worker_WithQueuedOperation_ShouldPersistQueuedCrawlingImportingSuccessInOrder()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var operation = await fixture.AddRefreshOperationAsync("queued");
        var crawlFinishedAt = operation.StartedAt.AddMinutes(1);
        var health = new MarketPulseAdminTestFixture.SequenceHealthService(
            new MarketPulseExternalSourceHealthDto
            {
                IsAvailable = true,
                Status = "degraded",
                CheckedAt = crawlFinishedAt,
                LatestListingStatus = "partial_success",
                LatestListingStartedAt = operation.StartedAt.AddSeconds(10),
                LatestListingFinishedAt = crawlFinishedAt,
                LatestListingJobsSeen = 25,
                ActiveJobs = 25
            });
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var importRunId = Guid.NewGuid();
        fixture.MarketPulseService.RefreshHandler = async _ =>
        {
            var importRun = new MarketPulsePipelineRun
            {
                MarketPulsePipelineRunId = importRunId,
                OperationType = "import",
                Status = "success",
                Mode = "jobs_api_pull",
                TriggerType = "manual",
                CurrentStep = "analytics",
                BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
                RequestedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                FetchedCount = 25,
                SourceTotalCount = 25,
                IsCompleteSync = true,
                SourceLatestSuccessAt = crawlFinishedAt,
                SavedCount = 25,
                ImportedCount = 20,
                UpdatedCount = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            fixture.Context.Set<MarketPulsePipelineRun>().Add(importRun);
            await fixture.Context.SaveChangesAsync();
            return new MarketPulseRefreshResultDto
            {
                RunId = importRunId,
                Status = "success",
                IsCompleteSync = true,
                IsSourceFresh = true,
                PostingsScraped = 25,
                PostingsInserted = 20,
                PostingsUpdated = 5
            };
        };
        var worker = fixture.CreateWorker(healthService: health, handler: handler);

        await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(worker);
        fixture.Context.ChangeTracker.Clear();
        var crawling = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == operation.MarketPulsePipelineRunId);
        Assert.Equal("crawling", crawling.Status);

        await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(worker);
        fixture.Context.ChangeTracker.Clear();
        var importing = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == operation.MarketPulsePipelineRunId);
        Assert.Equal("importing", importing.Status);
        Assert.Equal(crawlFinishedAt, importing.CrawlerSuccessAt);

        await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(worker);
        fixture.Context.ChangeTracker.Clear();
        var succeeded = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == operation.MarketPulsePipelineRunId);
        var importRunPersisted = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == importRunId);

        Assert.Equal("success", succeeded.Status);
        Assert.Equal("analytics", succeeded.CurrentStep);
        Assert.Equal(importRunId, succeeded.ImportRunId);
        Assert.NotNull(succeeded.FinishedAt);
        Assert.True(succeeded.StartedAt <= succeeded.CrawlerSuccessAt);
        Assert.True(succeeded.CrawlerSuccessAt <= succeeded.FinishedAt);
        Assert.Equal(25, importRunPersisted.FetchedCount);
        Assert.Equal(20, importRunPersisted.ImportedCount);
        Assert.Equal(5, importRunPersisted.UpdatedCount);
    }

    [Fact]
    public async Task TC243_CreateRefreshOperation_WhenAnyActiveOperationExists_ShouldReferenceExistingOperation()
    {
        foreach (var activeStatus in new[] { "queued", "crawling", "importing" })
        {
            await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
            var existing = await fixture.AddRefreshOperationAsync(activeStatus);

            var conflict = await Assert.ThrowsAsync<MarketPulseRefreshOperationConflictException>(
                () => fixture.AdminService.CreateRefreshOperationAsync(CancellationToken.None));
            var count = await fixture.Context.Set<MarketPulsePipelineRun>()
                .CountAsync(run => run.OperationType == "refresh");

            Assert.Equal(existing.MarketPulsePipelineRunId, conflict.CurrentOperation.OperationId);
            Assert.Equal(activeStatus, conflict.CurrentOperation.Status);
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task TC244_Worker_WhenCrawlerHealthIsCritical_ShouldFailOperationWithoutStartingImport()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var operation = await fixture.AddRefreshOperationAsync("crawling");
        var health = new MarketPulseAdminTestFixture.SequenceHealthService(
            new MarketPulseExternalSourceHealthDto
            {
                IsAvailable = false,
                Status = "critical",
                CheckedAt = DateTime.UtcNow,
                LatestListingStatus = "critical",
                LatestListingStartedAt = operation.StartedAt.AddSeconds(5),
                LatestListingFinishedAt = operation.StartedAt.AddMinutes(1),
                LatestListingJobsSeen = 0,
                ActiveJobs = 0,
                ErrorMessage = "Jobs API is unavailable."
            });
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var worker = fixture.CreateWorker(healthService: health, handler: handler);

        await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(worker);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == operation.MarketPulsePipelineRunId);

        Assert.Equal("failed", persisted.Status);
        Assert.Equal("CRAWLER_HEALTH_CRITICAL", persisted.ErrorCode);
        Assert.Contains("unavailable", persisted.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.MarketPulseService.RefreshCalls);
    }

    [Fact]
    public async Task TC245_Refresh_WhenLatestCrawlerObservationIsStale_ShouldBlockImportAndPreserveCanonicalData()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var existing = await fixture.AddPostingAsync("existing-stale-guard");
        var originalUpdatedAt = existing.UpdatedAt;
        var coverageEnd = DateTime.UtcNow.Date.AddDays(-1);
        fixture.Context.Set<MarketPulsePipelineRun>().Add(new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "history_sync",
            Status = "success",
            Mode = "history_sync",
            TriggerType = "history_sync",
            CurrentStep = "analytics",
            BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
            RequestedAt = DateTime.UtcNow.AddDays(-10),
            StartedAt = DateTime.UtcNow.AddDays(-10),
            FinishedAt = DateTime.UtcNow.AddDays(-10),
            CoverageStart = coverageEnd.AddDays(-30),
            CoverageEnd = coverageEnd,
            SourceDataAt = coverageEnd,
            LastSuccessfulSyncAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        });
        await fixture.Context.SaveChangesAsync();
        var staleSourceTime = DateTimeOffset.UtcNow.AddHours(-48);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(_ =>
            MarketPulseAdminTestFixture.JsonResponse(
                MarketPulseAdminTestFixture.CreateJobsApiPayload(
                    [MarketPulseAdminTestFixture.CreateJob("stale-new-job", updatedAt: staleSourceTime)],
                    staleSourceTime)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);

        await Assert.ThrowsAnyAsync<Exception>(
            () => service.RefreshAsync(CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        var importRun = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "import");
        var persistedExisting = await fixture.Context.Set<JobPosting>()
            .SingleAsync(posting => posting.JobPostingId == existing.JobPostingId);
        var history = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "history_sync");

        Assert.Equal("stale_source", importRun.Status);
        Assert.False(importRun.MissingLifecycleApplied);
        Assert.Equal("source_freshness_invalid", importRun.LifecycleSkippedReason);
        Assert.True(persistedExisting.IsActive);
        Assert.Equal(originalUpdatedAt, persistedExisting.UpdatedAt);
        Assert.Equal(coverageEnd, history.CoverageEnd);
        Assert.DoesNotContain(
            await fixture.Context.Set<JobPosting>().ToListAsync(),
            posting => posting.ExternalId.Contains("stale-new-job", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC246_Refresh_WithNewerCompleteObservation_ShouldPersistMetricsInvalidateCacheAndRemainIdempotent()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var olderObservation = DateTime.UtcNow.AddHours(-2);
        await fixture.AddImportRunAsync(sourceLatestSuccessAt: olderObservation);
        var sourceTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var payload = MarketPulseAdminTestFixture.CreateJobsApiPayload(
            [MarketPulseAdminTestFixture.CreateJob("new-complete-run", updatedAt: sourceTime)],
            sourceTime,
            total: 1,
            complete: true);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(
            _ => MarketPulseAdminTestFixture.JsonResponse(payload));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);
        var before = await service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Days = 30 },
            CancellationToken.None);

        var first = await service.RefreshAsync(CancellationToken.None);
        var after = await service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Days = 30 },
            CancellationToken.None);
        var second = await service.RefreshAsync(CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var postingCount = await fixture.Context.Set<JobPosting>().CountAsync();
        var latestRun = await fixture.Context.Set<MarketPulsePipelineRun>()
            .Where(run => run.OperationType == "import")
            .OrderByDescending(run => run.StartedAt)
            .FirstAsync();

        Assert.Equal(0, before.ActivePostings);
        Assert.Equal("success", first.Status);
        Assert.Equal(1, first.PostingsInserted);
        Assert.Equal(1, after.ActivePostings);
        Assert.Equal("skipped", second.Status);
        Assert.Equal("source_observation_not_newer", second.LifecycleSkippedReason);
        Assert.Equal(1, postingCount);
        Assert.Equal(sourceTime.UtcDateTime, latestRun.SourceLatestSuccessAt);
        Assert.True(latestRun.IsCompleteSync);
        Assert.Equal(1, latestRun.FetchedCount);
    }

    [Fact]
    public async Task TC247_Refresh_WithOlderObservation_ShouldSkipWithoutChangingCanonicalDataOrCoverage()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var newerObservation = DateTime.UtcNow.AddMinutes(-10);
        await fixture.AddImportRunAsync(sourceLatestSuccessAt: newerObservation);
        var existing = await fixture.AddPostingAsync("current-canonical");
        var originalHash = existing.ContentHash;
        var coverageEnd = DateTime.UtcNow.Date;
        fixture.Context.Set<MarketPulsePipelineRun>().Add(new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "history_sync",
            Status = "success",
            Mode = "history_sync",
            TriggerType = "history_sync",
            CurrentStep = "analytics",
            BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
            RequestedAt = DateTime.UtcNow.AddDays(-1),
            StartedAt = DateTime.UtcNow.AddDays(-1),
            FinishedAt = DateTime.UtcNow.AddDays(-1),
            CoverageStart = coverageEnd.AddDays(-30),
            CoverageEnd = coverageEnd,
            SourceDataAt = newerObservation,
            LastSuccessfulSyncAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await fixture.Context.SaveChangesAsync();
        var olderSourceTime = new DateTimeOffset(newerObservation.AddMinutes(-20), TimeSpan.Zero);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(_ =>
            MarketPulseAdminTestFixture.JsonResponse(
                MarketPulseAdminTestFixture.CreateJobsApiPayload(
                    [MarketPulseAdminTestFixture.CreateJob("older-observation", updatedAt: olderSourceTime)],
                    olderSourceTime)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);

        var result = await service.RefreshAsync(CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var postings = await fixture.Context.Set<JobPosting>().ToListAsync();
        var persisted = postings.Single(posting => posting.JobPostingId == existing.JobPostingId);
        var history = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "history_sync");

        Assert.Equal("skipped", result.Status);
        Assert.Equal("source_observation_not_newer", result.LifecycleSkippedReason);
        Assert.Single(postings);
        Assert.Equal(originalHash, persisted.ContentHash);
        Assert.True(persisted.IsActive);
        Assert.Equal(coverageEnd, history.CoverageEnd);
        Assert.Equal(newerObservation, history.SourceDataAt);
    }

    [Fact]
    public async Task TC248_Refresh_WithPartialSnapshot_ShouldNotDeactivateOmittedJobsOrAdvanceCoverage()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var omitted = await fixture.AddPostingAsync("omitted-active-job");
        var coverageEnd = DateTime.UtcNow.Date.AddDays(-1);
        fixture.Context.Set<MarketPulsePipelineRun>().Add(new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "history_sync",
            Status = "success",
            Mode = "history_sync",
            TriggerType = "history_sync",
            CurrentStep = "analytics",
            BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
            RequestedAt = DateTime.UtcNow.AddDays(-2),
            StartedAt = DateTime.UtcNow.AddDays(-2),
            FinishedAt = DateTime.UtcNow.AddDays(-2),
            CoverageStart = coverageEnd.AddDays(-30),
            CoverageEnd = coverageEnd,
            SourceDataAt = coverageEnd,
            LastSuccessfulSyncAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await fixture.Context.SaveChangesAsync();
        var sourceTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(_ =>
            MarketPulseAdminTestFixture.JsonResponse(
                MarketPulseAdminTestFixture.CreateJobsApiPayload(
                    [MarketPulseAdminTestFixture.CreateJob("partial-returned-job", updatedAt: sourceTime)],
                    sourceTime,
                    total: 2,
                    complete: false)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);

        var result = await service.RefreshAsync(CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var persistedOmitted = await fixture.Context.Set<JobPosting>()
            .SingleAsync(posting => posting.JobPostingId == omitted.JobPostingId);
        var history = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "history_sync");

        Assert.Equal("partial", result.Status);
        Assert.False(result.MissingLifecycleApplied);
        Assert.Equal("partial_sync", result.LifecycleSkippedReason);
        Assert.True(persistedOmitted.IsActive);
        Assert.Equal("active", persistedOmitted.LifecycleStatus);
        Assert.Equal(0, persistedOmitted.MissingScanCount);
        Assert.Equal(coverageEnd, history.CoverageEnd);
    }

    [Fact]
    public async Task TC249_Refresh_WithCompleteFreshNewerSnapshot_ShouldApplyMissingLifecycleAndAdvanceCoverage()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var omitted = await fixture.AddPostingAsync("omitted-complete-job");
        omitted.LastCheckedAt = DateTime.UtcNow.AddDays(-2);
        await fixture.Context.SaveChangesAsync();
        var sourceTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var coverageEnd = MarketPulseBusinessTime
            .GetBusinessDate(sourceTime, fixture.Settings.BusinessTimezone)
            .AddDays(-1)
            .ToDateTime(TimeOnly.MinValue);
        fixture.Context.Set<MarketPulsePipelineRun>().Add(new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "history_sync",
            Status = "success",
            Mode = "history_sync",
            TriggerType = "history_sync",
            CurrentStep = "analytics",
            BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
            RequestedAt = DateTime.UtcNow.AddDays(-2),
            StartedAt = DateTime.UtcNow.AddDays(-2),
            FinishedAt = DateTime.UtcNow.AddDays(-2),
            CoverageStart = coverageEnd.AddDays(-30),
            CoverageEnd = coverageEnd,
            SourceDataAt = coverageEnd,
            LastSuccessfulSyncAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await fixture.Context.SaveChangesAsync();
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(_ =>
            MarketPulseAdminTestFixture.JsonResponse(
                MarketPulseAdminTestFixture.CreateJobsApiPayload(
                    [MarketPulseAdminTestFixture.CreateJob("complete-returned-job", updatedAt: sourceTime)],
                    sourceTime,
                    total: 1,
                    complete: true)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);

        var result = await service.RefreshAsync(CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var persistedOmitted = await fixture.Context.Set<JobPosting>()
            .SingleAsync(posting => posting.JobPostingId == omitted.JobPostingId);
        var history = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "history_sync");

        Assert.Equal("success", result.Status);
        Assert.True(result.MissingLifecycleApplied);
        Assert.False(persistedOmitted.IsActive);
        Assert.Equal("stale_unverified", persistedOmitted.LifecycleStatus);
        Assert.Equal(1, persistedOmitted.MissingScanCount);
        Assert.Equal(
            MarketPulseBusinessTime.GetBusinessDate(sourceTime, fixture.Settings.BusinessTimezone)
                .ToDateTime(TimeOnly.MinValue),
            history.CoverageEnd);
    }

    [Fact]
    public async Task TC250_HistorySync_WithValidScope_ShouldImportIdempotentlyWithoutApplyingMissingLifecycle()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var omittedActive = await fixture.AddPostingAsync("active-not-in-history-page");
        var sourceTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var coverageStart = sourceTime.AddDays(-59);
        var payload = MarketPulseAdminTestFixture.CreateJobsApiPayload(
            [MarketPulseAdminTestFixture.CreateJob("historical-job", active: false, updatedAt: sourceTime.AddDays(-20))],
            sourceTime,
            total: 1,
            complete: true,
            historyCoverageStart: coverageStart,
            pageSize: 50);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(
            _ => MarketPulseAdminTestFixture.JsonResponse(payload));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = fixture.CreateRealMarketPulseService(handler, cache);
        var request = new MarketPulseHistorySyncRequestDto
        {
            LookbackDays = 60,
            JobsApiPageSize = 50,
            JobsApiMaxItems = 500
        };

        var first = await service.SyncPublicationHistoryAsync(request, CancellationToken.None);
        var second = await service.SyncPublicationHistoryAsync(request, CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var postings = await fixture.Context.Set<JobPosting>().ToListAsync();
        var persistedActive = postings.Single(posting => posting.JobPostingId == omittedActive.JobPostingId);
        var history = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.OperationType == "history_sync");

        Assert.Equal("success", first.Status);
        Assert.Equal("success", second.Status);
        Assert.Equal(2, postings.Count);
        Assert.True(persistedActive.IsActive);
        Assert.Equal(0, persistedActive.MissingScanCount);
        Assert.False(first.MissingLifecycleApplied);
        Assert.Equal("historical_sync", first.LifecycleSkippedReason);
        Assert.Equal(
            MarketPulseBusinessTime.GetBusinessDate(coverageStart, fixture.Settings.BusinessTimezone)
                .ToDateTime(TimeOnly.MinValue),
            history.CoverageStart);
        Assert.Equal(
            MarketPulseBusinessTime.GetBusinessDate(sourceTime, fixture.Settings.BusinessTimezone)
                .ToDateTime(TimeOnly.MinValue),
            history.CoverageEnd);
        Assert.Equal(1, history.SyncedPostingCount);
    }

    [Fact]
    public async Task TC252_RetryFailedItem_WhenRetrySucceeds_ShouldResolveItemWithoutDuplicatingCanonicalPosting()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var failure = await fixture.AddFailureAsync();
        await fixture.AddPostingAsync("retry-canonical-posting");
        var sourceTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var handler = new MarketPulseAdminTestFixture.RecordingHttpMessageHandler(_ =>
            MarketPulseAdminTestFixture.JsonResponse(
                MarketPulseAdminTestFixture.CreateJobsApiPayload(
                    [MarketPulseAdminTestFixture.CreateJob("retry-canonical-posting", updatedAt: sourceTime)],
                    sourceTime,
                    total: 1,
                    complete: true)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var realMarketPulseService = fixture.CreateRealMarketPulseService(handler, cache);
        var adminService = new MarketPulseAdminService(
            fixture.Context,
            realMarketPulseService,
            fixture.HealthService,
            fixture.TopCvClient);

        var result = await adminService.RetryFailedItemsAsync(
            [failure.MarketPulseFailedItemId],
            CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Set<MarketPulseFailedItem>()
            .SingleAsync(item => item.MarketPulseFailedItemId == failure.MarketPulseFailedItemId);
        var postings = await fixture.Context.Set<JobPosting>().ToListAsync();

        Assert.Equal("resolved", Assert.Single(result).Status);
        Assert.Equal("resolved", persisted.Status);
        Assert.Equal(1, persisted.RetryCount);
        Assert.NotNull(persisted.LastRetryAt);
        Assert.Single(postings);
        Assert.Equal("retry-canonical-posting", postings.Single().SourceJobId);
    }

    [Fact]
    public async Task TC255_ClassifierMapping_CreateUpdateDelete_ShouldChangeTestClassificationWithoutCreatingJob()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var beforeJobs = fixture.Context.JobPostings.Count();
        var created = await fixture.AdminService.CreateClassifierMappingAsync(
            new MarketPulseClassifierMappingRequestDto
            {
                Keyword = "quarkflux",
                Category = "Platform Engineering",
                IsEnabled = true,
                Weight = 5m
            },
            CancellationToken.None);

        var first = await fixture.AdminService.TestClassifierAsync(
            new MarketPulseClassifierTestRequestDto
            {
                Text = "Senior quarkflux engineer"
            },
            CancellationToken.None);
        var updated = await fixture.AdminService.UpdateClassifierMappingAsync(
            created.MappingId,
            new MarketPulseClassifierMappingRequestDto
            {
                Keyword = "quarkflux-next",
                Category = "DevOps",
                IsEnabled = true,
                Weight = 7m
            },
            CancellationToken.None);
        var oldKeywordResult = await fixture.AdminService.TestClassifierAsync(
            new MarketPulseClassifierTestRequestDto
            {
                Text = "Senior quarkflux engineer"
            },
            CancellationToken.None);
        var newKeywordResult = await fixture.AdminService.TestClassifierAsync(
            new MarketPulseClassifierTestRequestDto
            {
                Text = "Senior quarkflux-next engineer"
            },
            CancellationToken.None);
        await fixture.AdminService.DeleteClassifierMappingAsync(
            created.MappingId,
            CancellationToken.None);
        var deletedResult = await fixture.AdminService.TestClassifierAsync(
            new MarketPulseClassifierTestRequestDto
            {
                Text = "Senior quarkflux-next engineer"
            },
            CancellationToken.None);

        Assert.Equal("Platform Engineering", first.Category);
        Assert.Equal("quarkflux-next", updated.Keyword);
        Assert.Equal("Other", oldKeywordResult.Category);
        Assert.Equal("DevOps", newKeywordResult.Category);
        Assert.Equal("Other", deletedResult.Category);
        Assert.Equal(beforeJobs, fixture.Context.JobPostings.Count());
    }

    [Fact]
    public async Task TC259_RestartedWorker_WithPersistedImportingOperation_ShouldFinalizeOnceAndExposePostgresLockContract()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        var operation = await fixture.AddRefreshOperationAsync("importing");
        var importRunId = Guid.NewGuid();
        fixture.MarketPulseService.RefreshResult =
            MarketPulseAdminTestFixture.CreateSuccessfulRefreshResult(importRunId);

        var firstWorker = fixture.CreateWorker();
        await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(firstWorker);
        var secondWorker = fixture.CreateWorker();
        var secondProcessed = await MarketPulseAdminTestFixture.ProcessWorkerOnceAsync(secondWorker);

        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Set<MarketPulsePipelineRun>()
            .SingleAsync(run => run.MarketPulsePipelineRunId == operation.MarketPulsePipelineRunId);
        var activeCount = await fixture.Context.Set<MarketPulsePipelineRun>()
            .CountAsync(run =>
                run.OperationType == "refresh" &&
                (run.Status == "queued" || run.Status == "crawling" || run.Status == "importing"));
        var lockField = typeof(MarketPulseRefreshOperationWorker).GetField(
            "WorkerAdvisoryLockKey",
            BindingFlags.NonPublic | BindingFlags.Static);
        var acquireMethod = typeof(MarketPulseRefreshOperationWorker).GetMethod(
            "TryAcquireWorkerLockAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        var releaseMethod = typeof(MarketPulseRefreshOperationWorker).GetMethod(
            "ReleaseWorkerLockAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Equal("success", persisted.Status);
        Assert.Equal(importRunId, persisted.ImportRunId);
        Assert.Equal(1, fixture.MarketPulseService.RefreshCalls);
        Assert.False(secondProcessed);
        Assert.Equal(0, activeCount);
        Assert.NotNull(lockField);
        Assert.NotNull(acquireMethod);
        Assert.NotNull(releaseMethod);
    }
}
