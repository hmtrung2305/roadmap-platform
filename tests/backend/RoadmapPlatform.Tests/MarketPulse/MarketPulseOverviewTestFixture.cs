using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests.MarketPulse;

internal sealed class MarketPulseOverviewTestFixture : IAsyncDisposable
{
    private readonly MemoryCache cache;

    private MarketPulseOverviewTestFixture(
        ApplicationDbContext context,
        MarketPulseService service,
        MemoryCache cache,
        MarketPulseSettings settings)
    {
        Context = context;
        Service = service;
        this.cache = cache;
        Settings = settings;
        Today = MarketPulseBusinessTime.GetBusinessDate(DateTime.UtcNow, settings.BusinessTimezone);
    }

    public ApplicationDbContext Context { get; }

    public MarketPulseService Service { get; }

    public MarketPulseSettings Settings { get; }

    public DateOnly Today { get; }

    public static Task<MarketPulseOverviewTestFixture> CreateAsync()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"market-pulse-{Guid.NewGuid():N}")
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TestApplicationDbContext(dbOptions);
        var settings = new MarketPulseSettings
        {
            BusinessTimezone = MarketPulseBusinessTime.DefaultTimezone,
            OverviewCacheSeconds = 0,
            TrackedKeywords =
            [
                "React|React.js|ReactJS",
                "SQL|PostgreSQL|MySQL|SQL Server",
                "Docker|Kubernetes|K8s",
                "Python|Django|FastAPI|Flask",
                "Azure",
                "Java|Spring|Spring Boot",
                "Go|Golang",
                "TypeScript|TS"
            ]
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var jobsApiClient = new TopCvJobsApiClient(
            new NoNetworkHttpClientFactory(),
            Options.Create(settings),
            NullLogger<TopCvJobsApiClient>.Instance);
        var service = new MarketPulseService(
            context,
            jobsApiClient,
            new JobMarketOverviewBuilder(new JobMarketKeywordAnalyzer()),
            cache,
            Options.Create(settings));

        return Task.FromResult(new MarketPulseOverviewTestFixture(context, service, cache, settings));
    }

    public async Task<JobPosting> AddPostingAsync(
        string externalId,
        DateOnly publishedOn,
        string title = "Backend Engineer",
        string category = "Backend",
        string location = "Ho Chi Minh",
        string experience = "3 years",
        string? salary = "20 - 30 triệu",
        long? salaryMin = null,
        long? salaryMax = null,
        string? salaryCurrency = null,
        IReadOnlyCollection<string>? skills = null,
        bool isActive = true,
        string postDateConfidence = "exact")
    {
        var timestamp = publishedOn.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        var postingSkills = skills ?? ["React", "SQL"];
        var posting = new JobPosting
        {
            JobPostingId = Guid.NewGuid(),
            ExternalId = externalId,
            SourceJobId = externalId,
            Title = title,
            CompanyName = "Test Company",
            Category = category,
            Location = location,
            Salary = salary,
            SalaryRaw = salary,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            SalaryCurrency = salaryCurrency,
            SalaryIsNegotiable = false,
            Experience = experience,
            ExperienceRaw = experience,
            ExperienceMinYears = 3,
            ExperienceMaxYears = 3,
            Url = $"https://topcv.vn/jobs/{Uri.EscapeDataString(externalId)}",
            Description = $"{title} requiring {string.Join(", ", postingSkills)}.",
            PublishedAt = timestamp,
            PostDateText = publishedOn.ToString("yyyy-MM-dd"),
            PostDateConfidence = postDateConfidence,
            PostDateLowerBound = timestamp,
            PostDateUpperBound = timestamp,
            PostDateObservedOn = timestamp,
            SourceUpdatedAt = now,
            DetailStatus = "success",
            DetailLastSuccessAt = now,
            Requirements = JsonSerializer.Serialize(postingSkills),
            Specialties = JsonSerializer.Serialize(postingSkills),
            Benefits = JsonSerializer.Serialize(new[] { "Laptop" }),
            Skills = JsonSerializer.Serialize(postingSkills),
            ContentHash = Guid.NewGuid().ToString("N"),
            LifecycleStatus = isActive ? "active" : "expired",
            IsActive = isActive,
            MissingScanCount = 0,
            SeenCount = 1,
            UpdatedScanCount = 0,
            FirstSeenAt = now,
            LastSeenAt = now,
            LastCheckedAt = now,
            LastChangedAt = now,
            ScrapedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        Context.JobPostings.Add(posting);
        await Context.SaveChangesAsync();
        return posting;
    }

    public async Task AddHistoryCoverageAsync(
        int coverageDays = 240,
        DateTime? sourceDataAt = null)
    {
        var now = DateTime.UtcNow;
        var run = new MarketPulsePipelineRun
        {
            MarketPulsePipelineRunId = Guid.NewGuid(),
            OperationType = "history_sync",
            Status = "success",
            Mode = "history",
            TriggerType = "test",
            CurrentStep = "completed",
            BaselineCrawlerSuccessAt = now,
            CrawlerSuccessAt = now,
            RequestedAt = now,
            StartedAt = now,
            FinishedAt = now,
            DurationMs = 1,
            FetchedCount = Context.JobPostings.Count(),
            SourceTotalCount = Context.JobPostings.Count(),
            IsCompleteSync = true,
            MissingLifecycleApplied = false,
            SourceGeneratedAt = now,
            SourceLatestSuccessAt = now,
            SavedCount = Context.JobPostings.Count(),
            ImportedCount = Context.JobPostings.Count(),
            UpdatedCount = 0,
            SkippedCount = 0,
            DuplicateCount = 0,
            FailedCount = 0,
            CoverageStart = Today.AddDays(-(coverageDays - 1)).ToDateTime(TimeOnly.MinValue),
            CoverageEnd = Today.ToDateTime(TimeOnly.MinValue),
            SourceDataAt = sourceDataAt ?? now,
            LastSuccessfulSyncAt = now,
            SyncedPostingCount = Context.JobPostings.Count(),
            CreatedAt = now,
            UpdatedAt = now
        };

        Context.MarketPulsePipelineRuns.Add(run);
        await Context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        cache.Dispose();
    }

    private sealed class NoNetworkHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new NoNetworkHandler());
    }

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }

    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
        }
    }
}
