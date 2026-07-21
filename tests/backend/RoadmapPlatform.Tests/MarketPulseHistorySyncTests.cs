using System.Net;
using System.Text;
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

namespace RoadmapPlatform.Tests;

public sealed class MarketPulseHistorySyncTests
{
    [Fact]
    public async Task SameCrawlerWatermarkAppliesBackfillWithoutIncrementingSeenCounters()
    {
        await using var context = CreateContext();
        var latestCrawl = DateTimeOffset.UtcNow.AddHours(-1);
        var coverageStart = latestCrawl.AddDays(-30);
        var requestCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            requestCount++;
            var enriched = requestCount >= 2;
            var publishedDate = DateOnly.FromDateTime(latestCrawl.UtcDateTime).AddDays(-3);
            return JsonResponse(new
            {
                ok = true,
                data = new[]
                {
                    new
                    {
                        id = "topcv:history-1",
                        source = "topcv",
                        source_job_id = "history-1",
                        title = "Historical backend engineer",
                        url = "https://topcv.vn/jobs/history-1",
                        is_active = false,
                        post_date = enriched ? publishedDate.ToString("yyyy-MM-dd") : null,
                        post_date_text = enriched ? "3 days ago" : "unknown",
                        post_date_confidence = enriched ? "relative" : "unknown",
                        post_date_lower_bound = enriched ? publishedDate.ToString("yyyy-MM-dd") : null,
                        post_date_upper_bound = enriched ? publishedDate.ToString("yyyy-MM-dd") : null,
                        post_date_observed_on = enriched
                            ? DateOnly.FromDateTime(latestCrawl.UtcDateTime).ToString("yyyy-MM-dd")
                            : null,
                        updated_at = enriched
                            ? latestCrawl.AddMinutes(10).ToString("O")
                            : latestCrawl.AddMinutes(-10).ToString("O")
                    }
                },
                pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
                meta = new
                {
                    generatedAt = latestCrawl.AddMinutes(20),
                    latestSuccessfulCrawlAt = latestCrawl,
                    historyCoverageStart = coverageStart
                }
            });
        });
        var settings = new MarketPulseSettings
        {
            JobsApiUrl = "https://jobs.example.test/api/v1/jobs",
            JobsApiPageSize = 100,
            JobsApiMaxPages = 10,
            JobsApiMaxItems = 100,
            JobsApiMaxFreshnessHours = 24,
            JobsApiFailOnStaleSource = true,
            JobsApiRequireFreshCrawlMetadata = true,
            BusinessTimezone = MarketPulseBusinessTime.DefaultTimezone,
            HistoryLookbackDays = 400
        };
        var client = new TopCvJobsApiClient(
            new StubHttpClientFactory(new HttpClient(handler)),
            Options.Create(settings),
            NullLogger<TopCvJobsApiClient>.Instance);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new MarketPulseService(
            context,
            client,
            new JobMarketOverviewBuilder(new JobMarketKeywordAnalyzer()),
            cache,
            Options.Create(settings));

        await service.SyncPublicationHistoryAsync(null, CancellationToken.None);
        var initial = await context.Set<JobPosting>().SingleAsync();
        Assert.Equal(1, initial.SeenCount);
        Assert.Equal("unknown", initial.PostDateConfidence);

        await service.SyncPublicationHistoryAsync(null, CancellationToken.None);
        var enrichedPosting = await context.Set<JobPosting>().SingleAsync();
        var updatedAt = enrichedPosting.UpdatedAt;
        var updatedScanCount = enrichedPosting.UpdatedScanCount;
        Assert.Equal("relative", enrichedPosting.PostDateConfidence);
        Assert.Equal(1, enrichedPosting.SeenCount);
        Assert.Equal(1, updatedScanCount);

        await service.SyncPublicationHistoryAsync(null, CancellationToken.None);
        var repeated = await context.Set<JobPosting>().SingleAsync();
        Assert.Equal(1, repeated.SeenCount);
        Assert.Equal(updatedScanCount, repeated.UpdatedScanCount);
        Assert.Equal(updatedAt, repeated.UpdatedAt);

        var history = await context.Set<MarketPulsePipelineRun>()
            .SingleAsync(item => item.OperationType == "history_sync");
        Assert.Equal(
            MarketPulseBusinessTime.GetBusinessDate(coverageStart, settings.BusinessTimezone),
            DateOnly.FromDateTime(history.CoverageStart!.Value));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static HttpResponseMessage JsonResponse(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
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
