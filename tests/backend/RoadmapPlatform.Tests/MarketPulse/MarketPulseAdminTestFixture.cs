using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Application.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests.MarketPulse;

internal sealed class MarketPulseAdminTestFixture : IAsyncDisposable
{
    private readonly MemoryCache cache;

    private MarketPulseAdminTestFixture(
        ApplicationDbContext context,
        StubMarketPulseService marketPulseService,
        SequenceHealthService healthService,
        RecordingHttpMessageHandler httpHandler,
        MarketPulseSettings settings,
        MemoryCache cache)
    {
        Context = context;
        MarketPulseService = marketPulseService;
        HealthService = healthService;
        HttpHandler = httpHandler;
        Settings = settings;
        this.cache = cache;
        TopCvClient = new TopCvJobsApiClient(
            new StubHttpClientFactory(new HttpClient(httpHandler)),
            Options.Create(settings),
            NullLogger<TopCvJobsApiClient>.Instance);
        AdminService = new MarketPulseAdminService(
            context,
            marketPulseService,
            healthService,
            TopCvClient);
    }

    public ApplicationDbContext Context { get; }

    public StubMarketPulseService MarketPulseService { get; }

    public SequenceHealthService HealthService { get; }

    public RecordingHttpMessageHandler HttpHandler { get; }

    public MarketPulseSettings Settings { get; }

    public TopCvJobsApiClient TopCvClient { get; }

    public MarketPulseAdminService AdminService { get; }

    public static Task<MarketPulseAdminTestFixture> CreateAsync(
        MarketPulseExternalSourceHealthDto? health = null,
        MarketPulseOverviewDto? overview = null,
        MarketPulseRefreshResultDto? refreshResult = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"market-pulse-admin-{Guid.NewGuid():N}")
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TestApplicationDbContext(options);
        var settings = CreateDefaultSettings();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var marketPulseService = new StubMarketPulseService
        {
            OverviewResult = overview ?? CreateOverview(),
            RefreshResult = refreshResult ?? CreateSuccessfulRefreshResult()
        };
        var healthService = new SequenceHealthService(
            health ?? CreateHealthyHealth());
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = Array.Empty<object>(),
            pagination = new { total = 0, page = 1, pageSize = 50, totalPages = 0 }
        }));

        return Task.FromResult(new MarketPulseAdminTestFixture(
            context,
            marketPulseService,
            healthService,
            handler,
            settings,
            cache));
    }

    public MarketPulseService CreateRealMarketPulseService(
        HttpMessageHandler handler,
        IMemoryCache memoryCache)
    {
        var client = new TopCvJobsApiClient(
            new StubHttpClientFactory(new HttpClient(handler)),
            Options.Create(Settings),
            NullLogger<TopCvJobsApiClient>.Instance);
        return new MarketPulseService(
            Context,
            client,
            new JobMarketOverviewBuilder(new JobMarketKeywordAnalyzer()),
            memoryCache,
            Options.Create(Settings));
    }

    public MarketPulseRefreshOperationWorker CreateWorker(
        IMarketPulseService? marketPulseService = null,
        IJobsApiHealthService? healthService = null,
        HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Context);
        services.AddSingleton<IMarketPulseService>(marketPulseService ?? MarketPulseService);
        services.AddSingleton<IJobsApiHealthService>(healthService ?? HealthService);
        var client = new TopCvJobsApiClient(
            new StubHttpClientFactory(new HttpClient(handler ?? HttpHandler)),
            Options.Create(Settings),
            NullLogger<TopCvJobsApiClient>.Instance);
        services.AddSingleton(client);
        var provider = services.BuildServiceProvider();
        return new MarketPulseRefreshOperationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(Settings),
            NullLogger<MarketPulseRefreshOperationWorker>.Instance);
    }

    public static async Task<bool> ProcessWorkerOnceAsync(
        MarketPulseRefreshOperationWorker worker,
        CancellationToken cancellationToken = default)
    {
        var method = typeof(MarketPulseRefreshOperationWorker).GetMethod(
            "ProcessNextAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task<bool>>(method.Invoke(worker, [cancellationToken]));
        return await task;
    }

    public async Task<MarketPulsePipelineRun> AddRefreshOperationAsync(
        string status,
        DateTime? requestedAt = null)
    {
        var now = requestedAt ?? DateTime.UtcNow.AddMinutes(-2);
        var operation = new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "refresh",
            Status = status,
            Mode = "end_to_end",
            TriggerType = "manual",
            CurrentStep = status == "importing" ? "import" : "crawler",
            BaselineCrawlerSuccessAt = now.AddMinutes(-5),
            RequestedAt = now,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        Context.Set<MarketPulsePipelineRun>().Add(operation);
        await Context.SaveChangesAsync();
        return operation;
    }

    public async Task<MarketPulsePipelineRun> AddImportRunAsync(
        string status = "success",
        DateTime? startedAt = null,
        DateTime? sourceLatestSuccessAt = null,
        bool complete = true,
        int fetched = 10,
        int inserted = 4,
        int updated = 3,
        int failed = 0)
    {
        var started = startedAt ?? DateTime.UtcNow.AddMinutes(-5);
        var finished = started.AddMinutes(1);
        var run = new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "import",
            Status = status,
            Mode = "jobs_api_pull",
            TriggerType = "manual",
            CurrentStep = "analytics",
            BaselineCrawlerSuccessAt = DateTime.UnixEpoch,
            RequestedAt = started,
            StartedAt = started,
            FinishedAt = finished,
            DurationMs = 60_000,
            FetchedCount = fetched,
            SourceTotalCount = fetched,
            IsCompleteSync = complete,
            SourceGeneratedAt = sourceLatestSuccessAt ?? finished,
            SourceLatestSuccessAt = sourceLatestSuccessAt ?? finished,
            SavedCount = inserted + updated,
            ImportedCount = inserted,
            UpdatedCount = updated,
            FailedCount = failed,
            MissingLifecycleApplied = complete,
            CreatedAt = started,
            UpdatedAt = finished
        };
        Context.Set<MarketPulsePipelineRun>().Add(run);
        await Context.SaveChangesAsync();
        return run;
    }

    public async Task<MarketPulseFailedItem> AddFailureAsync(string status = "open")
    {
        var now = DateTime.UtcNow;
        var item = new MarketPulseFailedItem
        {
            MarketPulseFailedItemId = Guid.NewGuid(),
            Url = "https://topcv.vn/jobs/failed-item",
            Stage = "import",
            ErrorCode = "TEST_FAILURE",
            ErrorMessage = "Synthetic import failure",
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
        Context.Set<MarketPulseFailedItem>().Add(item);
        await Context.SaveChangesAsync();
        return item;
    }

    public async Task<JobPosting> AddPostingAsync(
        string externalId,
        bool active = true,
        int missingScanCount = 0,
        DateTime? publishedAt = null)
    {
        var now = DateTime.UtcNow;
        var posting = new JobPosting
        {
            JobPostingId = Guid.NewGuid(),
            ExternalId = externalId,
            SourceJobId = externalId,
            Title = $"Job {externalId}",
            CompanyName = "Test Company",
            Category = "Backend",
            Location = "Ho Chi Minh",
            Salary = "20 - 30 triệu",
            SalaryRaw = "20 - 30 triệu",
            Experience = "3 years",
            ExperienceRaw = "3 years",
            Url = $"https://topcv.vn/jobs/{externalId}",
            Description = "Backend role using .NET and PostgreSQL.",
            PublishedAt = publishedAt ?? now.AddDays(-1),
            PostDateText = "1 day ago",
            PostDateConfidence = "exact",
            PostDateLowerBound = publishedAt ?? now.AddDays(-1),
            PostDateUpperBound = publishedAt ?? now.AddDays(-1),
            PostDateObservedOn = now,
            SourceUpdatedAt = now,
            DetailStatus = "success",
            DetailLastSuccessAt = now,
            Requirements = JsonSerializer.Serialize(new[] { ".NET" }),
            Specialties = JsonSerializer.Serialize(new[] { "Backend" }),
            Benefits = JsonSerializer.Serialize(new[] { "Laptop" }),
            Skills = JsonSerializer.Serialize(new[] { ".NET", "PostgreSQL" }),
            ContentHash = Guid.NewGuid().ToString("N"),
            LifecycleStatus = active ? "active" : "expired",
            IsActive = active,
            MissingScanCount = missingScanCount,
            SeenCount = 1,
            UpdatedScanCount = 0,
            FirstSeenAt = now.AddDays(-5),
            LastSeenAt = now.AddDays(-1),
            LastCheckedAt = now.AddDays(-1),
            LastChangedAt = now.AddDays(-1),
            ScrapedAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-1)
        };
        Context.Set<JobPosting>().Add(posting);
        await Context.SaveChangesAsync();
        return posting;
    }

    public static MarketPulseSettings CreateDefaultSettings() => new()
    {
        JobsApiUrl = "https://jobs.example.test/api/v1/jobs",
        JobsApiOpsHealthUrl = "https://jobs.example.test/api/v1/ops/health-summary",
        JobsApiCrawlTriggerUrl = "https://jobs.example.test/api/crawl/listing/run",
        JobsApiKey = "super-secret-test-key",
        JobsApiPageSize = 100,
        JobsApiMaxPages = 10,
        JobsApiMaxItems = 1000,
        JobsApiMaxFreshnessHours = 24,
        JobsApiFailOnStaleSource = true,
        JobsApiRequireFreshCrawlMetadata = true,
        MissingScansBeforeStale = 1,
        MinimumPostingsForLifecycleCheck = 1,
        DisableMissingLifecycleForPartialSync = true,
        BusinessTimezone = MarketPulseBusinessTime.DefaultTimezone,
        OverviewCacheSeconds = 600,
        HistoryLookbackDays = 90,
        RefreshOperationTimeoutMinutes = 30
    };

    public static MarketPulseExternalSourceHealthDto CreateHealthyHealth(DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;
        return new MarketPulseExternalSourceHealthDto
        {
            IsAvailable = true,
            Status = "healthy",
            CheckedAt = timestamp,
            GeneratedAt = timestamp,
            LatestSuccessfulCrawlAt = timestamp.AddMinutes(-5),
            HoursSinceSuccessfulCrawl = 0.1,
            LatestListingStatus = "success",
            LatestListingStartedAt = timestamp.AddMinutes(-7),
            LatestListingFinishedAt = timestamp.AddMinutes(-5),
            LatestListingJobsSeen = 100,
            ActiveJobs = 100,
            DetailCompletionRate = 95m
        };
    }

    public static MarketPulseOverviewDto CreateOverview()
    {
        return new MarketPulseOverviewDto
        {
            ActivePostings = 100,
            PublicationAnalytics = new MarketPulsePublicationAnalyticsDto
            {
                Availability = "available",
                Confidence = "high",
                CurrentPeriod = new MarketPulsePublicationPeriodDto
                {
                    EstimatedTotal = 25
                },
                PostDateQuality = new MarketPulsePostDateQualityDto
                {
                    ReliablePercent = 90
                },
                MarketTrendPoints =
                [
                    new MarketPulsePublicationTrendPointDto
                    {
                        Date = DateTime.UtcNow.Date,
                        Available = true,
                        TotalEstimate = 5
                    }
                ]
            },
            DataQuality = new MarketDataQualityDto
            {
                Level = "healthy",
                DetailCoveragePercent = 95,
                SalaryCoveragePercent = 80
            }
        };
    }

    public static MarketPulseRefreshResultDto CreateSuccessfulRefreshResult(Guid? runId = null) => new()
    {
        RunId = runId ?? Guid.NewGuid(),
        Status = "success",
        IsCompleteSync = true,
        IsSourceFresh = true,
        PostingsScraped = 10,
        PostingsInserted = 4,
        PostingsUpdated = 3,
        ActivePostings = 100
    };

    public static HttpResponseMessage JsonResponse(object payload, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    public static object CreateJobsApiPayload(
        IReadOnlyCollection<object> jobs,
        DateTimeOffset sourceTime,
        int? total = null,
        bool complete = true,
        DateTimeOffset? historyCoverageStart = null,
        int pageSize = 100) => new
    {
        ok = true,
        data = jobs,
        pagination = new
        {
            total = total ?? jobs.Count,
            page = 1,
            pageSize,
            totalPages = 1
        },
        meta = new
        {
            generatedAt = sourceTime.AddMinutes(1).ToString("O"),
            latestSuccessfulCrawlAt = sourceTime.ToString("O"),
            latestDataCrawlAt = sourceTime.ToString("O"),
            isSourceComplete = complete,
            historyCoverageStart = (historyCoverageStart ?? sourceTime.AddDays(-30)).ToString("O")
        }
    };

    public static object CreateJob(
        string id,
        bool active = true,
        DateTimeOffset? updatedAt = null,
        string? title = null) => new
    {
        id = $"topcv:{id}",
        source = "topcv",
        source_job_id = id,
        title = title ?? $"Backend Engineer {id}",
        company_name = "Test Company",
        url = $"https://topcv.vn/jobs/{id}",
        is_active = active,
        category = "Backend",
        location = "Ho Chi Minh",
        salary = "20 - 30 triệu",
        experience = "3 years",
        description = "Backend role using .NET and PostgreSQL.",
        skills = new[] { ".NET", "PostgreSQL" },
        post_date = DateOnly.FromDateTime((updatedAt ?? DateTimeOffset.UtcNow).UtcDateTime).ToString("yyyy-MM-dd"),
        post_date_text = "today",
        post_date_confidence = "exact",
        post_date_lower_bound = DateOnly.FromDateTime((updatedAt ?? DateTimeOffset.UtcNow).UtcDateTime).ToString("yyyy-MM-dd"),
        post_date_upper_bound = DateOnly.FromDateTime((updatedAt ?? DateTimeOffset.UtcNow).UtcDateTime).ToString("yyyy-MM-dd"),
        post_date_observed_on = DateOnly.FromDateTime((updatedAt ?? DateTimeOffset.UtcNow).UtcDateTime).ToString("yyyy-MM-dd"),
        updated_at = (updatedAt ?? DateTimeOffset.UtcNow).ToString("O")
    };

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        cache.Dispose();
    }

    internal sealed class StubMarketPulseService : IMarketPulseService
    {
        public MarketPulseOverviewDto OverviewResult { get; set; } = new();

        public MarketPulseRefreshResultDto RefreshResult { get; set; } = new();

        public MarketPulseRefreshResultDto HistoryResult { get; set; } = new();

        public Func<CancellationToken, Task<MarketPulseRefreshResultDto>>? RefreshHandler { get; set; }

        public Func<MarketPulseHistorySyncRequestDto?, CancellationToken, Task<MarketPulseRefreshResultDto>>? HistoryHandler { get; set; }

        public int RefreshCalls { get; private set; }

        public int HistoryCalls { get; private set; }

        public Task<MarketPulseOverviewDto> GetOverviewAsync(
            MarketPulseOverviewQueryDto query,
            CancellationToken cancellationToken) => Task.FromResult(OverviewResult);

        public Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken)
        {
            RefreshCalls++;
            return RefreshHandler is null
                ? Task.FromResult(RefreshResult)
                : RefreshHandler(cancellationToken);
        }

        public Task<MarketPulseRefreshResultDto> RefreshAsync(
            MarketPulseRefreshRequestDto? request,
            CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

        public Task<MarketPulseRefreshResultDto> IngestAsync(
            MarketPulseIngestRequestDto request,
            CancellationToken cancellationToken) => Task.FromResult(RefreshResult);

        public Task<MarketPulseRefreshResultDto> SyncPublicationHistoryAsync(
            MarketPulseHistorySyncRequestDto? request,
            CancellationToken cancellationToken)
        {
            HistoryCalls++;
            return HistoryHandler is null
                ? Task.FromResult(HistoryResult)
                : HistoryHandler(request, cancellationToken);
        }
    }

    internal sealed class SequenceHealthService(params MarketPulseExternalSourceHealthDto[] responses)
        : IJobsApiHealthService
    {
        private readonly Queue<MarketPulseExternalSourceHealthDto> queue = new(responses);
        private MarketPulseExternalSourceHealthDto last = responses.LastOrDefault() ?? new();

        public int Calls { get; private set; }

        public Task<MarketPulseExternalSourceHealthDto> GetHealthAsync(
            CancellationToken cancellationToken)
        {
            Calls++;
            if (queue.Count > 0)
            {
                last = queue.Dequeue();
            }
            return Task.FromResult(last);
        }
    }

    internal sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Calls++;
                Requests.Add(request);
            }
            return Task.FromResult(responseFactory(request));
        }
    }

    internal sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    internal sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
        }
    }
}
