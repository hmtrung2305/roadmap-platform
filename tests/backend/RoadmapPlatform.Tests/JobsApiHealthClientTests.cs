using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class JobsApiHealthClientTests
{
    [Fact]
    public async Task GetHealthAsync_MapsCrawlerAndFreshnessHealth()
    {
        string? apiKey = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            apiKey = request.Headers.TryGetValues("X-API-Key", out var values)
                ? values.Single()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "ok": true,
                      "data": {
                        "pipeline_status": "warning",
                        "generated_at": "2026-07-17T10:00:00",
                        "latest_listing_run": {
                          "status": "partial_success",
                          "started_at": "2026-07-17T09:00:00",
                          "finished_at": "2026-07-17T09:10:00",
                          "pages_blocked": 1,
                          "pages_failed": 0,
                          "unique_jobs_seen": 420
                        },
                        "data_quality": {
                          "active_jobs": 420,
                          "new_jobs_today": 18,
                          "detail_completion_rate": 0.91
                        },
                        "freshness": {
                          "latest_successful_listing_run_at": "2026-07-17T08:00:00",
                          "hours_since_successful_listing": 2,
                          "latest_data_listing_run_at": "2026-07-17T09:10:00",
                          "hours_since_data_listing": 0.83,
                          "is_source_complete": false
                        },
                        "warnings": ["warning: one page was blocked."]
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var client = CreateClient(handler);

        var result = await client.GetHealthAsync(CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.Equal("warning", result.Status);
        Assert.False(result.IsStale);
        Assert.False(result.IsBlocked);
        Assert.Equal("partial_success", result.LatestListingStatus);
        Assert.Equal(420, result.LatestListingJobsSeen);
        Assert.Equal(new DateTime(2026, 7, 17, 9, 10, 0), result.LatestSuccessfulCrawlAt);
        Assert.Equal(420, result.ActiveJobs);
        Assert.Equal("test-key", apiKey);
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsUnavailableWithoutThrowingOnUpstreamFailure()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await client.GetHealthAsync(CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.True(result.IsStale);
        Assert.Equal("http_error", result.Status);
    }

    [Fact]
    public async Task GetHealthAsync_MarksHealthyPipelineStaleWhenLastCrawlIsTooOld()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "ok": true,
                      "data": {
                        "pipeline_status": "healthy",
                        "data_quality": {
                          "active_jobs": 100,
                          "new_jobs_today": 2,
                          "detail_completion_rate": 0.8
                        },
                        "freshness": {
                          "latest_successful_listing_run_at": "2026-07-15T09:00:00",
                          "hours_since_successful_listing": 49
                        }
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }));

        var result = await client.GetHealthAsync(CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsStale);
        Assert.Equal("stale", result.Status);
    }

    private static JobsApiHealthClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        return new JobsApiHealthClient(
            new StubHttpClientFactory(httpClient),
            Options.Create(new MarketPulseSettings
            {
                JobsApiOpsHealthUrl = "https://jobs.example.test/api/v1/ops/health-summary",
                JobsApiHealthTimeoutSeconds = 5,
                JobsApiMaxFreshnessHours = 24,
                JobsApiKey = "test-key"
            }),
            NullLogger<JobsApiHealthClient>.Instance);
    }

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
