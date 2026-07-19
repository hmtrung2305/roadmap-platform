using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class MarketPulseAdminRetryTests
{
    [Fact]
    public void RefreshWorkerAcceptsPartialCrawlerRunWhenItCommittedJobs()
    {
        var method = typeof(MarketPulseRefreshOperationWorker).GetMethod(
            "IsUsablePartialCrawlerResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var usable = new MarketPulseExternalSourceHealthDto
        {
            LatestListingStatus = "partial_success",
            LatestListingFinishedAt = DateTime.UtcNow,
            LatestListingJobsSeen = 50,
            PagesBlocked = 1
        };
        var blockedBeforeData = new MarketPulseExternalSourceHealthDto
        {
            LatestListingStatus = "blocked",
            LatestListingFinishedAt = DateTime.UtcNow,
            LatestListingJobsSeen = 0,
            PagesBlocked = 1
        };

        Assert.True((bool)method.Invoke(null, [usable])!);
        Assert.False((bool)method.Invoke(null, [blockedBeforeData])!);
    }

    [Fact]
    public async Task RefreshOperationFailureIsPersistedAfterSharedTrackerWasCleared()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var operation = new MarketPulseRefreshOperation
        {
            MarketPulseRefreshOperationId = Guid.NewGuid(),
            Status = "importing",
            CurrentStep = "import",
            BaselineCrawlerSuccessAt = now.AddMinutes(-5),
            RequestedAt = now.AddMinutes(-5),
            StartedAt = now.AddMinutes(-4),
            UpdatedAt = now.AddMinutes(-1)
        };
        context.Set<MarketPulseRefreshOperation>().Add(operation);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var failMethod = typeof(MarketPulseRefreshOperationWorker).GetMethod(
            "FailAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(failMethod);
        var detached = new MarketPulseRefreshOperation
        {
            MarketPulseRefreshOperationId = operation.MarketPulseRefreshOperationId,
            Status = "importing"
        };

        var task = Assert.IsAssignableFrom<Task>(failMethod.Invoke(
            null,
            [context, detached, "PIPELINE_FAILED", "Synthetic import failure", CancellationToken.None]));
        await task;

        context.ChangeTracker.Clear();
        var persisted = await context.Set<MarketPulseRefreshOperation>().SingleAsync();
        Assert.Equal("failed", persisted.Status);
        Assert.Equal("PIPELINE_FAILED", persisted.ErrorCode);
        Assert.Equal("Synthetic import failure", persisted.ErrorMessage);
        Assert.NotNull(persisted.FinishedAt);
    }

    [Fact]
    public async Task RetryImportFailuresExecutesOneImportAndResolvesSelectedRows()
    {
        await using var context = CreateContext();
        var failure = CreateFailure();
        context.Set<MarketPulseFailedItem>().Add(failure);
        await context.SaveChangesAsync();
        var marketPulse = new StubMarketPulseService(new MarketPulseRefreshResultDto
        {
            Status = "success",
            IsCompleteSync = true,
            IsSourceFresh = true
        });
        var service = CreateAdminService(context, marketPulse);

        var result = await service.RetryFailedItemsAsync(
            [failure.MarketPulseFailedItemId],
            CancellationToken.None);

        Assert.Equal(1, marketPulse.RefreshCalls);
        Assert.Equal("resolved", Assert.Single(result).Status);
        Assert.Equal(1, result.Single().RetryCount);
        Assert.NotNull(result.Single().LastRetryAt);
    }

    [Fact]
    public async Task BusyImportRestoresSelectedFailureToOpen()
    {
        await using var context = CreateContext();
        var failure = CreateFailure();
        context.Set<MarketPulseFailedItem>().Add(failure);
        await context.SaveChangesAsync();
        var marketPulse = new StubMarketPulseService(
            new InvalidOperationException("A Market Pulse refresh is already running."));
        var service = CreateAdminService(context, marketPulse);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryFailedItemsAsync(
                [failure.MarketPulseFailedItemId],
                CancellationToken.None));

        var persisted = await context.Set<MarketPulseFailedItem>()
            .SingleAsync(item => item.MarketPulseFailedItemId == failure.MarketPulseFailedItemId);
        Assert.Equal("open", persisted.Status);
        Assert.Equal(1, persisted.RetryCount);
    }

    [Fact]
    public async Task RetryDoesNotChangeIgnoredRowsFromMixedRequest()
    {
        await using var context = CreateContext();
        var open = CreateFailure();
        var ignored = CreateFailure();
        ignored.Status = "ignored";
        context.Set<MarketPulseFailedItem>().AddRange(open, ignored);
        await context.SaveChangesAsync();
        var service = CreateAdminService(
            context,
            new StubMarketPulseService(new MarketPulseRefreshResultDto
            {
                Status = "success",
                IsCompleteSync = true,
                IsSourceFresh = true
            }));

        var result = await service.RetryFailedItemsAsync(
            [open.MarketPulseFailedItemId, ignored.MarketPulseFailedItemId],
            CancellationToken.None);

        Assert.Equal("resolved", Assert.Single(result).Status);
        var persistedIgnored = await context.Set<MarketPulseFailedItem>()
            .SingleAsync(item => item.MarketPulseFailedItemId == ignored.MarketPulseFailedItemId);
        Assert.Equal("ignored", persistedIgnored.Status);
        Assert.Equal(0, persistedIgnored.RetryCount);
    }

    private static MarketPulseAdminService CreateAdminService(
        ApplicationDbContext context,
        IMarketPulseService marketPulse) => new(
            context,
            marketPulse,
            new StubHealthService(),
            new TopCvJobsApiClient(
                new StubHttpClientFactory(),
                Options.Create(new MarketPulseSettings()),
                NullLogger<TopCvJobsApiClient>.Instance));

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static MarketPulseFailedItem CreateFailure()
    {
        var now = DateTime.UtcNow;
        return new MarketPulseFailedItem
        {
            MarketPulseFailedItemId = Guid.NewGuid(),
            Stage = "import",
            ErrorCode = "TEST_FAILURE",
            ErrorMessage = "Synthetic import failure",
            Status = "open",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class StubMarketPulseService : IMarketPulseService
    {
        private readonly MarketPulseRefreshResultDto? result;
        private readonly Exception? exception;

        public StubMarketPulseService(MarketPulseRefreshResultDto result) => this.result = result;

        public StubMarketPulseService(Exception exception) => this.exception = exception;

        public int RefreshCalls { get; private set; }

        public Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken)
        {
            RefreshCalls++;
            return exception is null
                ? Task.FromResult(result!)
                : Task.FromException<MarketPulseRefreshResultDto>(exception);
        }

        public Task<MarketPulseRefreshResultDto> RefreshAsync(
            MarketPulseRefreshRequestDto? request,
            CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

        public Task<MarketPulseOverviewDto> GetOverviewAsync(
            MarketPulseOverviewQueryDto query,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MarketPulseRefreshResultDto> IngestAsync(
            MarketPulseIngestRequestDto request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MarketPulseRefreshResultDto> SyncPublicationHistoryAsync(
            MarketPulseHistorySyncRequestDto? request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubHealthService : IJobsApiHealthService
    {
        public Task<MarketPulseExternalSourceHealthDto> GetHealthAsync(
            CancellationToken cancellationToken) => Task.FromResult(new MarketPulseExternalSourceHealthDto());
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
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
