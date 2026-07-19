using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class TopCvJobsApiClientTests
{
    [Fact]
    public async Task FetchAsync_RejectsNonTopCvProviderContract()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = new[]
            {
                new
                {
                    id = "other:1",
                    source = "other",
                    title = "Unsupported",
                    url = "https://example.test/1",
                    is_active = true
                }
            },
            pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
            meta = new
            {
                generatedAt = now,
                latestSuccessfulCrawlAt = now
            }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("unsupported provider", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_ReturnsSuccess_AndSendsApiKey()
    {
        string? apiKey = null;
        var now = DateTimeOffset.UtcNow;
        var historyCoverageStart = now.AddDays(-45);
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
                        salary = "20 - 35 trieu",
                        salary_raw = "20 - 35 triệu",
                        salary_min = 20_000_000L,
                        salary_max = 35_000_000L,
                        salary_currency = "VND",
                        salary_is_negotiable = false,
                        experience = "3 - 5 nam",
                        experience_raw = "3 - 5 năm",
                        experience_min_years = 3,
                        experience_max_years = 5,
                        skills_normalized = new[] { "Python", "SQL" },
                        requirements = new[] { "Python" },
                        benefits = new[] { "Laptop" },
                        specialties = new[] { "FastAPI" },
                        post_date = "2026-06-18",
                        post_date_text = "Đăng 1 giờ trước",
                        post_date_confidence = "relative",
                        post_date_lower_bound = "2026-06-11",
                        post_date_upper_bound = "2026-06-18",
                        post_date_observed_on = "2026-06-18",
                        detail_status = "success",
                        detail_last_success_at = "2026-06-18T09:00:00Z",
                        first_seen_at = "2026-05-04T02:30:00Z",
                        url = "https://topcv.vn/jobs/1001",
                        is_active = true
                    }
                },
                pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
                meta = new
                {
                    generatedAt = now.AddMinutes(-1),
                    latestSuccessfulCrawlAt = now.AddHours(-1),
                    historyCoverageStart
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
        Assert.Equal(historyCoverageStart, result.HistoryCoverageStart);
        var job = result.Jobs.Single();
        Assert.Equal("topcv", job.Source);
        Assert.Equal("20 - 35 triệu", job.SalaryRaw);
        Assert.Equal(20_000_000L, job.SalaryMin);
        Assert.Equal(35_000_000L, job.SalaryMax);
        Assert.Equal("VND", job.SalaryCurrency);
        Assert.False(job.SalaryIsNegotiable);
        Assert.Equal("3 - 5 năm", job.ExperienceRaw);
        Assert.Equal(3, job.ExperienceMinYears);
        Assert.Equal(5, job.ExperienceMaxYears);
        Assert.Equal(["Python", "SQL"], job.SkillsNormalized);
        Assert.Equal(["Python"], job.Requirements);
        Assert.Equal(["Laptop"], job.Benefits);
        Assert.Equal(["FastAPI"], job.Specialties);
        Assert.Equal("relative", job.PostDateConfidence);
        Assert.Equal("2026-06-11", job.PostDateLowerBound);
        Assert.Equal("2026-06-18", job.PostDateUpperBound);
        Assert.Equal("2026-06-18", job.PostDateObservedOn);
        Assert.Equal("success", job.DetailStatus);
        Assert.Equal("2026-06-18T09:00:00Z", job.DetailLastSuccessAt);
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
    public async Task FetchAsync_ImportsUsablePartialCrawlerDataWithoutClaimingLifecycleSafety()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = new[]
            {
                new
                {
                    id = "topcv:partial-1",
                    source = "topcv",
                    source_job_id = "partial-1",
                    title = "Usable partial role",
                    url = "https://topcv.vn/jobs/partial-1",
                    is_active = true
                }
            },
            pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
            meta = new
            {
                generatedAt = now.AddMinutes(-1),
                latestSuccessfulCrawlAt = (DateTimeOffset?)null,
                latestDataCrawlAt = now.AddMinutes(-5),
                isSourceComplete = false
            }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Success, result.Status);
        Assert.True(result.IsSourceFresh);
        Assert.False(result.IsSourceComplete);
        Assert.False(result.IsCompleteSync);
        Assert.Equal(1, result.FetchedCount);
        Assert.Equal(now.AddMinutes(-5), result.LatestSuccessfulCrawlAt);
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

    [Fact]
    public async Task TriggerListingCrawl_TreatsBusyCrawlerAsIdempotentAttachment()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)),
            settings => settings.JobsApiCrawlTriggerUrl =
                "https://jobs.example.test/api/crawl/listing/run");

        var result = await client.TriggerListingCrawlAsync(CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Contains("already running", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerListingCrawl_DerivesEndpointFromJobsApiUrl()
    {
        Uri? requestedUri = null;
        var client = CreateClient(
            new StubHttpMessageHandler(request =>
            {
                requestedUri = request.RequestUri;
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }),
            settings => settings.JobsApiUrl =
                "http://localhost:8000/api/v1/jobs?scope=active");

        var result = await client.TriggerListingCrawlAsync(CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("/api/crawl/listing/run", requestedUri?.AbsolutePath);
        Assert.True(string.IsNullOrEmpty(requestedUri?.Query));
    }

    [Fact]
    public async Task GetOpenCrawlerFailureCount_UsesPaginationTotalsAcrossStatuses()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(request =>
            {
                var query = request.RequestUri?.Query ?? string.Empty;
                var total = query.Contains("status=open", StringComparison.OrdinalIgnoreCase)
                    ? 3
                    : query.Contains("status=retrying", StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 2;
                return JsonResponse(new
                {
                    ok = true,
                    data = Array.Empty<object>(),
                    pagination = new { total }
                });
            }),
            settings => settings.JobsApiUrl = "https://jobs.example.test/api/v1/jobs");

        var result = await client.GetOpenCrawlerFailureCountAsync(CancellationToken.None);

        Assert.Equal(6, result);
    }

    [Fact]
    public async Task FetchAsync_RejectsDuplicateStableIdsAcrossPages()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(request =>
        {
            var page = request.RequestUri?.Query.Contains("page=2", StringComparison.Ordinal) == true
                ? 2
                : 1;
            return JsonResponse(new
            {
                ok = true,
                data = new[]
                {
                    new
                    {
                        id = "topcv:duplicate",
                        source = "topcv",
                        url = "https://topcv.vn/jobs/duplicate",
                        is_active = true
                    }
                },
                pagination = new { total = 2, page, pageSize = 1, totalPages = 2 },
                meta = new { generatedAt = now, latestSuccessfulCrawlAt = now }
            });
        }));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            new JobsApiFetchOptions(2, 1, 2),
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("duplicate", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_RejectsUnexpectedPaginationCoordinates()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(request =>
        {
            var requestedSecondPage = request.RequestUri?.Query.Contains(
                "page=2",
                StringComparison.Ordinal) == true;
            var page = requestedSecondPage ? 99 : 1;
            var id = requestedSecondPage ? "topcv:2" : "topcv:1";
            return JsonResponse(new
            {
                ok = true,
                data = new[] { new { id, source = "topcv", url = $"https://topcv.vn/jobs/{id}", is_active = true } },
                pagination = new { total = 2, page, pageSize = 1, totalPages = 2 },
                meta = new { generatedAt = now, latestSuccessfulCrawlAt = now }
            });
        }));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            new JobsApiFetchOptions(2, 1, 2),
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("expected page=2", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_RejectsCrawlerWatermarkChangeAcrossPages()
    {
        var firstWatermark = DateTimeOffset.UtcNow.AddHours(-1);
        var client = CreateClient(new StubHttpMessageHandler(request =>
        {
            var page = request.RequestUri?.Query.Contains("page=2", StringComparison.Ordinal) == true
                ? 2
                : 1;
            var watermark = page == 1 ? firstWatermark : firstWatermark.AddMinutes(5);
            return JsonResponse(new
            {
                ok = true,
                data = new[]
                {
                    new
                    {
                        id = $"topcv:{page}",
                        source = "topcv",
                        url = $"https://topcv.vn/jobs/{page}",
                        is_active = true
                    }
                },
                pagination = new { total = 2, page, pageSize = 1, totalPages = 2 },
                meta = new { generatedAt = DateTimeOffset.UtcNow, latestSuccessfulCrawlAt = watermark }
            });
        }));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            new JobsApiFetchOptions(2, 1, 2),
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("latestSuccessfulCrawlAt changed", result.ErrorMessage);
        Assert.False(result.IsCompleteSync);
    }

    [Fact]
    public async Task HistoryFetch_NormalizesLegacyActiveOnlyUrl()
    {
        Uri? requestedUri = null;
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(
            new StubHttpMessageHandler(request =>
            {
                requestedUri = request.RequestUri;
                return JsonResponse(new
                {
                    ok = true,
                    data = Array.Empty<object>(),
                    pagination = new { total = 0, page = 1, pageSize = 100, totalPages = 0 },
                    meta = new { generatedAt = now, latestSuccessfulCrawlAt = now }
                });
            }),
            settings => settings.ActiveJobsApiUrl =
                "https://jobs.example.test/api/v1/jobs/active?active=true");

        var result = await client.FetchImportBatchAsync(
            null,
            TopCvJobScope.All,
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.Empty, result.Status);
        Assert.Equal("/api/v1/jobs", requestedUri?.AbsolutePath);
        Assert.DoesNotContain("active=", requestedUri?.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope=all", requestedUri?.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_RejectsJobWithoutUrlBeforeCompletenessIsGranted()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(new
        {
            ok = true,
            data = new[] { new { id = "topcv:missing-url", source = "topcv", is_active = true } },
            pagination = new { total = 1, page = 1, pageSize = 100, totalPages = 1 },
            meta = new { generatedAt = now, latestSuccessfulCrawlAt = now }
        })));

        var result = await client.FetchAsync(
            "https://jobs.example.test/api/v1/jobs",
            CancellationToken.None);

        Assert.Equal(JobsApiFetchStatus.InvalidContract, result.Status);
        Assert.Contains("without a URL", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.IsCompleteSync);
    }

    private static TopCvJobsApiClient CreateClient(
        HttpMessageHandler handler,
        Action<MarketPulseSettings>? configure = null)
    {
        var settings = new MarketPulseSettings
        {
            RetryMax = 1,
            BackoffBaseMs = 250,
            JobsApiMaxFreshnessHours = 24,
            JobsApiFailOnStaleSource = true,
            JobsApiRequireFreshCrawlMetadata = true
        };
        configure?.Invoke(settings);

        return new TopCvJobsApiClient(
            new StubHttpClientFactory(new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            }),
            Options.Create(settings),
            NullLogger<TopCvJobsApiClient>.Instance);
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
