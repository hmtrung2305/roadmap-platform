using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class MarketPulseRefreshFailureTests
{
    [Fact]
    public async Task GetOverviewAsync_CountsOnlyReliableDatesOnVietnamBusinessDate()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTime.UtcNow;
        var businessDate = MarketPulseBusinessTime.GetBusinessDate(
            now,
            MarketPulseBusinessTime.DefaultTimezone);
        var source = new JobPortalSource
        {
            JobPortalSourceId = Guid.NewGuid(),
            Name = "topcv",
            BaseUrl = "https://topcv.vn",
            SearchUrlTemplate = string.Empty,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Set<JobPortalSource>().Add(source);
        dbContext.Set<JobPosting>().AddRange(
            CreateOverviewPosting(source, "exact-today", businessDate, "exact", now),
            CreateOverviewPosting(source, "relative-today", businessDate, "relative", now),
            CreateOverviewPosting(source, "unknown-today", businessDate, "unknown", now),
            CreateOverviewPosting(source, "missing-date", null, "exact", now),
            CreateOverviewPosting(source, "exact-yesterday", businessDate.AddDays(-1), "exact", now));
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            new JobPortalScrapeResult { Status = JobsApiFetchStatus.Empty },
            settings =>
            {
                settings.BusinessTimezone = MarketPulseBusinessTime.DefaultTimezone;
                settings.OverviewCacheSeconds = 0;
            });

        var overview = await service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Days = 14 },
            CancellationToken.None);

        Assert.Equal(2, overview.TodayPostings);
        Assert.Equal(2, overview.TodayJobs.Count);
        Assert.Equal(
            ["exact-today", "relative-today"],
            overview.TodayJobs.Select(x => x.Title).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task RefreshAsync_PersistsExpandedJobsApiContractFields()
    {
        await using var dbContext = CreateDbContext();
        var detailLastSuccessAt = new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc);
        var postDateLowerBound = new DateTime(2026, 6, 11);
        var postDateUpperBound = new DateTime(2026, 6, 18);
        var postDateObservedOn = new DateTime(2026, 6, 18);
        var posting = new ScrapedJobPosting(
            "topcv",
            "Senior Backend Engineer",
            "Example Tech",
            "Ha Noi",
            "https://topcv.vn/jobs/expanded-contract",
            "Python SQL FastAPI",
            new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc),
            null,
            SourceJobId: "expanded-contract",
            Category: "Backend",
            Salary: "20 - 35 trieu",
            Experience: "3 - 5 nam",
            PostDateText: "Dang 1 gio truoc",
            Requirements: ["Python"],
            Specialties: ["FastAPI"],
            Benefits: ["Laptop"],
            Skills: ["Python", "SQL"],
            SalaryRaw: "20 - 35 triệu",
            SalaryMin: 20_000_000,
            SalaryMax: 35_000_000,
            SalaryCurrency: "vnd",
            SalaryIsNegotiable: false,
            ExperienceRaw: "3 - 5 năm",
            ExperienceMinYears: 3,
            ExperienceMaxYears: 5,
            PostDateConfidence: "relative",
            PostDateLowerBound: postDateLowerBound,
            PostDateUpperBound: postDateUpperBound,
            PostDateObservedOn: postDateObservedOn,
            DetailStatus: "success",
            DetailLastSuccessAt: detailLastSuccessAt);
        var service = CreateService(
            dbContext,
            new JobPortalScrapeResult
            {
                Status = JobsApiFetchStatus.Success,
                Total = 1,
                FetchedCount = 1,
                IsCompleteSync = true,
                IsSourceFresh = true,
                Postings = [posting]
            });

        await service.RefreshAsync(CancellationToken.None);

        var saved = await dbContext.Set<JobPosting>().SingleAsync();
        Assert.Equal("20 - 35 triệu", saved.SalaryRaw);
        Assert.Equal(20_000_000, saved.SalaryMin);
        Assert.Equal(35_000_000, saved.SalaryMax);
        Assert.Equal("VND", saved.SalaryCurrency);
        Assert.False(saved.SalaryIsNegotiable);
        Assert.Equal("3 - 5 năm", saved.ExperienceRaw);
        Assert.Equal(3, saved.ExperienceMinYears);
        Assert.Equal(5, saved.ExperienceMaxYears);
        Assert.Equal("relative", saved.PostDateConfidence);
        Assert.Equal(postDateLowerBound, saved.PostDateLowerBound);
        Assert.Equal(postDateUpperBound, saved.PostDateUpperBound);
        Assert.Equal(postDateObservedOn, saved.PostDateObservedOn);
        Assert.Equal("success", saved.DetailStatus);
        Assert.Equal(detailLastSuccessAt, saved.DetailLastSuccessAt);
        Assert.Equal("[\"Python\",\"SQL\"]", saved.Skills);
    }

    [Fact]
    public async Task RefreshAsync_PartialSync_DoesNotMarkAbsentPostingMissing()
    {
        await using var dbContext = CreateDbContext();
        var absentPostingId = await SeedAbsentPostingAsync(dbContext);
        var postings = CreatePostings(80);
        var service = CreateService(
            dbContext,
            new JobPortalScrapeResult
            {
                Status = JobsApiFetchStatus.Success,
                Total = 1_000,
                FetchedCount = 80,
                IsCompleteSync = false,
                IsSourceFresh = true,
                Postings = postings
            },
            settings =>
            {
                settings.MissingScansBeforeStale = 1;
                settings.MinimumPostingsForLifecycleCheck = 1;
            });

        var result = await service.RefreshAsync(CancellationToken.None);

        var absentPosting = await dbContext.Set<JobPosting>().SingleAsync(x => x.JobPostingId == absentPostingId);
        var run = await dbContext.Set<MarketPulseCrawlRun>().SingleAsync();
        Assert.True(absentPosting.IsActive);
        Assert.Equal("active", absentPosting.LifecycleStatus);
        Assert.Equal(0, absentPosting.MissingScanCount);
        Assert.False(result.MissingLifecycleApplied);
        Assert.Equal("partial_sync", result.LifecycleSkippedReason);
        Assert.False(run.MissingLifecycleApplied);
        Assert.Equal("partial_sync", run.LifecycleSkippedReason);
    }

    [Fact]
    public async Task RefreshAsync_CompleteSync_MayApplyMissingLifecycle()
    {
        await using var dbContext = CreateDbContext();
        var absentPostingId = await SeedAbsentPostingAsync(dbContext);
        var postings = CreatePostings(80);
        var service = CreateService(
            dbContext,
            new JobPortalScrapeResult
            {
                Status = JobsApiFetchStatus.Success,
                Total = 80,
                FetchedCount = 80,
                IsCompleteSync = true,
                IsSourceFresh = true,
                Postings = postings
            },
            settings =>
            {
                settings.MissingScansBeforeStale = 1;
                settings.MinimumPostingsForLifecycleCheck = 1;
            });

        var result = await service.RefreshAsync(CancellationToken.None);

        var absentPosting = await dbContext.Set<JobPosting>().SingleAsync(x => x.JobPostingId == absentPostingId);
        var run = await dbContext.Set<MarketPulseCrawlRun>().SingleAsync();
        Assert.False(absentPosting.IsActive);
        Assert.Equal("stale_unverified", absentPosting.LifecycleStatus);
        Assert.Equal(1, absentPosting.MissingScanCount);
        Assert.True(result.MissingLifecycleApplied);
        Assert.Null(result.LifecycleSkippedReason);
        Assert.True(run.MissingLifecycleApplied);
        Assert.Null(run.LifecycleSkippedReason);
    }

    [Fact]
    public async Task RefreshAsync_ReleasesLock_WhenStartingRunFails()
    {
        await using var dbContext = CreateFailingOnceDbContext();
        var service = CreateService(
            dbContext,
            new JobPortalScrapeResult
            {
                Status = JobsApiFetchStatus.Empty,
                ErrorMessage = "No active jobs were returned."
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync(CancellationToken.None));

        var result = await service.RefreshAsync(CancellationToken.None);

        Assert.Equal("empty", result.Status);
    }

    [Fact]
    public async Task RefreshAsync_RecordsFailedRun_AndDoesNotPersistJobs_OnHttpError()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(
            dbContext,
            JobPortalScrapeResult.Failure(
                JobsApiFetchStatus.HttpError,
                "Jobs API returned HTTP 503."));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.RefreshAsync(CancellationToken.None));

        var run = await dbContext.Set<MarketPulseCrawlRun>().SingleAsync();
        Assert.Equal("failed", run.Status);
        Assert.Equal("HttpError", run.StoppedReason);
        Assert.Empty(await dbContext.Set<JobPosting>().ToListAsync());
        Assert.Equal(
            "HttpError",
            (await dbContext.Set<MarketPulseFailedItem>().SingleAsync()).ErrorCode);
    }

    [Fact]
    public async Task RefreshAsync_RecordsStaleSource_AndDoesNotPersistJobs()
    {
        await using var dbContext = CreateDbContext();
        var staleResult = new JobPortalScrapeResult
        {
            Status = JobsApiFetchStatus.StaleSource,
            Total = 100,
            FetchedCount = 80,
            GeneratedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            LatestSuccessfulCrawlAt = DateTimeOffset.UtcNow.AddHours(-25),
            ErrorMessage = "Python crawler data is stale.",
            IsCompleteSync = false,
            Postings =
            [
                new ScrapedJobPosting(
                    "topcv",
                    "Should not be imported",
                    "Example",
                    "Ho Chi Minh",
                    "https://topcv.vn/jobs/not-imported",
                    "Should not be imported",
                    DateTime.UtcNow,
                    null)
            ]
        };
        var service = CreateService(dbContext, staleResult);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.RefreshAsync(CancellationToken.None));

        var run = await dbContext.Set<MarketPulseCrawlRun>().SingleAsync();
        Assert.Equal("stale_source", run.Status);
        Assert.Equal(100, run.SourceTotalCount);
        Assert.Equal(80, run.FetchedCount);
        Assert.Empty(await dbContext.Set<JobPosting>().ToListAsync());
        Assert.Equal(
            "stale_source",
            (await dbContext.Set<MarketPulseSourceHealth>().SingleAsync()).Status);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"market-pulse-phase-1-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateFailingOnceDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"market-pulse-start-failure-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new FailingOnceApplicationDbContext(options);
    }

    private static MarketPulseService CreateService(
        ApplicationDbContext dbContext,
        JobPortalScrapeResult result,
        Action<MarketPulseSettings>? configure = null)
    {
        var analyzer = new JobMarketKeywordAnalyzer();
        var settings = new MarketPulseSettings();
        configure?.Invoke(settings);
        return new MarketPulseService(
            dbContext,
            new StubJobPortalScraper(result),
            new JobMarketOverviewBuilder(analyzer),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(settings));
    }

    private static IReadOnlyList<ScrapedJobPosting> CreatePostings(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new ScrapedJobPosting(
                "topcv",
                $"Job {index}",
                "Example",
                "Ho Chi Minh",
                $"https://topcv.vn/jobs/{index}",
                $"Description {index}",
                DateTime.UtcNow,
                null,
                $"job-{index}"))
            .ToList();

    private static JobPosting CreateOverviewPosting(
        JobPortalSource source,
        string externalId,
        DateOnly? postDate,
        string confidence,
        DateTime now) =>
        new()
        {
            JobPostingId = Guid.NewGuid(),
            JobPortalSourceId = source.JobPortalSourceId,
            JobPortalSource = source,
            ExternalId = externalId,
            SourceJobId = externalId,
            Title = externalId,
            Url = $"https://topcv.vn/jobs/{externalId}",
            Description = externalId,
            PublishedAt = postDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            PostDateConfidence = confidence,
            Requirements = "[]",
            Specialties = "[]",
            Benefits = "[]",
            Skills = "[]",
            ContentHash = externalId,
            LifecycleStatus = "active",
            IsActive = true,
            MissingScanCount = 0,
            SeenCount = 1,
            FirstSeenAt = now,
            LastSeenAt = now,
            LastCheckedAt = now,
            ScrapedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static async Task<Guid> SeedAbsentPostingAsync(ApplicationDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var source = new JobPortalSource
        {
            JobPortalSourceId = Guid.NewGuid(),
            Name = "topcv",
            BaseUrl = "https://topcv.vn",
            SearchUrlTemplate = string.Empty,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var posting = new JobPosting
        {
            JobPostingId = Guid.NewGuid(),
            JobPortalSourceId = source.JobPortalSourceId,
            ExternalId = "absent-job",
            SourceJobId = "absent-job",
            Title = "Absent job",
            Url = "https://topcv.vn/jobs/absent-job",
            Description = "Existing posting omitted by the next import.",
            Requirements = "[]",
            Specialties = "[]",
            Benefits = "[]",
            Skills = "[]",
            ContentHash = "existing-content",
            LifecycleStatus = "active",
            IsActive = true,
            MissingScanCount = 0,
            SeenCount = 1,
            FirstSeenAt = now.AddDays(-2),
            LastSeenAt = now.AddDays(-1),
            LastCheckedAt = now.AddDays(-1),
            ScrapedAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-1)
        };

        dbContext.Set<JobPortalSource>().Add(source);
        dbContext.Set<JobPosting>().Add(posting);
        await dbContext.SaveChangesAsync();
        return posting.JobPostingId;
    }

    private sealed class StubJobPortalScraper(JobPortalScrapeResult result) : IJobPortalScraper
    {
        public Task<JobPortalScrapeResult> ScrapeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<JobPortalScrapeResult> ScrapeAsync(
            MarketPulseRefreshRequestDto? request,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private class TestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(x => x.Embedding);
        }
    }

    private sealed class FailingOnceApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options) : TestApplicationDbContext(options)
    {
        private bool failNextSave = true;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (failNextSave)
            {
                failNextSave = false;
                throw new InvalidOperationException("Simulated failure while starting an import run.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
