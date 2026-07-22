using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Api.Controllers.MarketPulse;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.MarketPulse;

public sealed class MarketPulseInternalApiTests
{
    private const string ConfiguredKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task TC257_InternalEndpoints_WithMissingShortOrWrongKey_ShouldRejectWithoutCreatingDurableWork()
    {
        var service = new RecordingMarketPulseService();
        var invalidKeys = new string?[]
        {
            null,
            "short-key",
            "fedcba9876543210fedcba9876543210"
        };

        foreach (var invalidKey in invalidKeys)
        {
            var refreshController = CreateController(service, invalidKey);
            var refreshResult = await refreshController.Refresh(CancellationToken.None);

            var ingestController = CreateController(service, invalidKey);
            var ingestResult = await ingestController.Ingest(
                CreateTc257ValidRequest(),
                CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(refreshResult);
            Assert.IsType<UnauthorizedResult>(ingestResult);
        }

        Assert.Equal(0, service.RefreshCalls);
        Assert.Equal(0, service.IngestCalls);
    }

    private static InternalMarketPulseController CreateController(
        IMarketPulseService service,
        string? providedKey)
    {
        var settings = new MarketPulseSettings
        {
            InternalApiKey = ConfiguredKey
        };
        var httpContext = new DefaultHttpContext();
        if (providedKey is not null)
        {
            httpContext.Request.Headers["X-Market-Pulse-Key"] = providedKey;
        }

        return new InternalMarketPulseController(service, Options.Create(settings))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static MarketPulseIngestRequestDto CreateTc257ValidRequest() => new()
    {
        SourceName = "topcv",
        Postings =
        [
            new MarketPulseIngestPostingDto
            {
                Id = "topcv:tc257",
                SourceJobId = "tc257",
                Title = "Backend Engineer",
                Company = "Test Company",
                Url = "https://topcv.vn/jobs/tc257",
                IsActive = true
            }
        ]
    };

    private sealed class RecordingMarketPulseService : IMarketPulseService
    {
        public int RefreshCalls { get; private set; }

        public int IngestCalls { get; private set; }

        public Task<MarketPulseOverviewDto> GetOverviewAsync(
            MarketPulseOverviewQueryDto query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MarketPulseOverviewDto());

        public Task<MarketPulseRefreshResultDto> RefreshAsync(
            CancellationToken cancellationToken)
        {
            RefreshCalls++;
            return Task.FromResult(new MarketPulseRefreshResultDto());
        }

        public Task<MarketPulseRefreshResultDto> RefreshAsync(
            MarketPulseRefreshRequestDto? request,
            CancellationToken cancellationToken) =>
            RefreshAsync(cancellationToken);

        public Task<MarketPulseRefreshResultDto> IngestAsync(
            MarketPulseIngestRequestDto request,
            CancellationToken cancellationToken)
        {
            IngestCalls++;
            return Task.FromResult(new MarketPulseRefreshResultDto());
        }

        public Task<MarketPulseRefreshResultDto> SyncPublicationHistoryAsync(
            MarketPulseHistorySyncRequestDto? request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MarketPulseRefreshResultDto());
    }

    private const string InternalKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task TC258_Ingest_WithValidKeyAndRepeatedPayload_ShouldRemainIdempotent()
    {
        await using var fixture = await MarketPulseAdminTestFixture.CreateAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        fixture.Settings.InternalApiKey = InternalKey;
        var service = fixture.CreateRealMarketPulseService(
            fixture.HttpHandler,
            cache);
        var request = CreateTc258ValidRequest();

        var firstController = CreateController(service, fixture.Settings);
        var firstResult = await firstController.Ingest(request, CancellationToken.None);

        var secondController = CreateController(service, fixture.Settings);
        var secondResult = await secondController.Ingest(request, CancellationToken.None);

        var firstOk = Assert.IsType<OkObjectResult>(firstResult);
        var secondOk = Assert.IsType<OkObjectResult>(secondResult);
        var firstPayload = Assert.IsType<MarketPulseRefreshResultDto>(firstOk.Value);
        var secondPayload = Assert.IsType<MarketPulseRefreshResultDto>(secondOk.Value);

        Assert.Equal(1, firstPayload.PostingsInserted);
        Assert.Equal(0, secondPayload.PostingsInserted);

        var postings = await fixture.Context.Set<JobPosting>()
            .AsNoTracking()
            .Where(item => item.ExternalId == "topcv:tc258")
            .ToListAsync();
        Assert.Single(postings);
        Assert.True(postings[0].IsActive);

        var activeOperations = await fixture.Context.Set<MarketPulsePipelineRun>()
            .AsNoTracking()
            .CountAsync(item =>
                item.OperationType == "refresh" &&
                (item.Status == "queued" ||
                 item.Status == "crawling" ||
                 item.Status == "importing"));
        Assert.Equal(0, activeOperations);
    }

    private static InternalMarketPulseController CreateController(
        RoadmapPlatform.Application.Interfaces.MarketPulse.IMarketPulseService service,
        RoadmapPlatform.Infrastructure.Configurations.MarketPulseSettings settings)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Market-Pulse-Key"] = InternalKey;

        return new InternalMarketPulseController(service, Options.Create(settings))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static MarketPulseIngestRequestDto CreateTc258ValidRequest() => new()
    {
        SourceName = "TopCV",
        Postings =
        [
            new MarketPulseIngestPostingDto
            {
                Id = "topcv:tc258",
                SourceJobId = "tc258",
                Title = "Senior Backend Engineer",
                Company = "Example Company",
                Category = "Backend",
                Location = "Ho Chi Minh City",
                Salary = "20 - 30 triệu",
                Experience = "3 years",
                PublishedAt = DateTime.UtcNow.Date.AddDays(-1),
                SourceUpdatedAt = DateTime.UtcNow.Date,
                Url = "https://topcv.vn/jobs/tc258",
                Description = "Backend role using .NET and PostgreSQL.",
                IsActive = true,
                Skills = [".NET", "PostgreSQL"]
            }
        ]
    };
}
