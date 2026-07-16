using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class JobsApiClientTests
{
    [Fact]
    public async Task FetchAsync_ReturnsSuccess_AndSendsApiKey()
    {
        string? apiKey = null;
        var now = DateTimeOffset.UtcNow;
        var handler = new StubHttpMessageHandler(request =>
        {
            apiKey = request.Headers.TryGetValues("X-API-Key", out var values)
                ? values.Single()
                : null;

            return JsonResponse(new
            {
                ok = true,
                data = new[]
                {
                    new
                    {
                        id = "topcv:1001",
                        source = "topcv",
                        source_job_id = "1001",
                        title = "Backend Engineer",
                        url = "https://topcv.vn/jobs/1001",
                        is_active = true
                    }
                },
                pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
                meta = new
                {
                    generatedAt = now.AddMinutes(-1),
                    latestSuccessfulCrawlAt = now.AddHours(-1)
                }
            });
        });
        var client = CreateClient(handler, settings =>
        {
            settings.JobsApiKey = "test-api-key";
        });

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Success, result.Status);
        Assert.Equal("test-api-key", apiKey);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.FetchedCount);
        Assert.True(result.IsCompleteSync);
        Assert.True(result.IsSourceFresh);
        Assert.Equal("topcv", result.Jobs.Single().Source);
    }

    [Fact]
    public async Task FetchAsync_ReportsPartialSync_WhenFetchedCountIsBelowTotal()
    {
        var now = DateTimeOffset.UtcNow;
        var jobs = Enumerable.Range(1, 100).Select(index => new
        {
            id = $"topcv:{index}",
            source = "topcv",
            source_job_id = index.ToString(),
            title = $"Job {index}",
            url = $"https://topcv.vn/jobs/{index}",
            is_active = true
        });
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = jobs,
            pagination = new { total = 1_000, page = 1, pageSize = 100, totalPages = 10 },
            meta = new
            {
                generatedAt = now.AddMinutes(-1),
                latestSuccessfulCrawlAt = now.AddHours(-1)
            }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            new JobsApiFetchOptions(80, 100, 1),
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Success, result.Status);
        Assert.Equal(1_000, result.Total);
        Assert.Equal(80, result.FetchedCount);
        Assert.False(result.IsCompleteSync);
        Assert.True(result.IsSourceFresh);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmpty_OnlyWithValidFreshness()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = Array.Empty<object>(),
            pagination = new { total = 0, page = 1, pageSize = 100, totalPages = 0 },
            meta = new
            {
                generatedAt = now.AddMinutes(-1),
                latestSuccessfulCrawlAt = now.AddHours(-1)
            }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Empty, result.Status);
        Assert.True(result.IsCompleteSync);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public async Task FetchAsync_ReturnsHttpError_ForUnsuccessfulResponse()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            settings => settings.RetryMax = 1);

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.HttpError, result.Status);
        Assert.Contains("503", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsTimeout_WhenRequestTimesOut()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => throw new TaskCanceledException("simulated timeout")),
            settings => settings.RetryMax = 1);

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Timeout, result.Status);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_ReturnsInvalidContract_WhenOkIsNotTrue()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = false,
            data = Array.Empty<object>(),
            pagination = new { total = 0, page = 1, pageSize = 100, totalPages = 0 },
            meta = new
            {
                generatedAt = DateTimeOffset.UtcNow,
                latestSuccessfulCrawlAt = DateTimeOffset.UtcNow
            }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("ok=true", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsInvalidContract_ForInvalidJson()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
            }));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("invalid JSON", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_ReturnsInvalidContract_WhenFreshnessMetadataIsMissing()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = Array.Empty<object>(),
            pagination = new { total = 0, page = 1, pageSize = 100, totalPages = 0 }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("meta", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_ReturnsStaleSource_WhenLatestCrawlIsTooOld()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(
            new StubHttpMessageHandler(_ => JsonResponse(new
            {
                ok = true,
                data = Array.Empty<object>(),
                pagination = new { total = 0, page = 1, pageSize = 100, totalPages = 0 },
                meta = new
                {
                    generatedAt = now.AddMinutes(-1),
                    latestSuccessfulCrawlAt = now.AddHours(-25)
                }
            })),
            settings => settings.JobsApiMaxFreshnessHours = 24);

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.StaleSource, result.Status);
        Assert.Contains("stale", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static JobsApiClient CreateClient(
        HttpMessageHandler handler,
        Action<MarketPulseSettings>? configure = null)
    {
        var settings = new MarketPulseSettings
        {
            RetryMax = 1,
            BackoffBaseMs = 250,
            DelayMinMs = 0,
            DelayMaxMs = 0,
            JobsApiMaxFreshnessHours = 24,
            JobsApiFailOnStaleSource = true,
            JobsApiRequireFreshCrawlMetadata = true
        };
        configure?.Invoke(settings);

        return new JobsApiClient(
            new StubHttpClientFactory(new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            }),
            Options.Create(settings),
            NullLogger<JobsApiClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(object payload) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
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
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
