using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public enum JobsApiFetchStatus
{
    Success,
    Empty,
    HttpError,
    Timeout,
    InvalidContract,
    StaleSource
}

public sealed class JobsApiFetchResult
{
    public JobsApiFetchStatus Status { get; init; }

    public IReadOnlyList<JobsApiJobDto> Jobs { get; init; } = [];

    public int? Total { get; init; }

    public int FetchedCount { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }

    public DateTimeOffset? LatestSuccessfulCrawlAt { get; init; }

    public DateTimeOffset? HistoryCoverageStart { get; init; }

    public bool IsSourceComplete { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsCompleteSync { get; init; }

    public bool IsSourceFresh { get; init; }
}

public sealed class TopCvJobsApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketPulseSettings> settings,
    ILogger<TopCvJobsApiClient> logger)
{
    public const string Provider = "topcv";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<(bool Accepted, string? Error)> TriggerListingCrawlAsync(
        CancellationToken cancellationToken)
    {
        var url = ResolveCrawlTriggerUrl();
        if (url is null)
        {
            return (
                false,
                "TopCV crawl trigger URL is not configured and could not be derived from MarketPulse:JobsApiUrl.");
        }

        var client = httpClientFactory.CreateClient("market-pulse");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        if (!string.IsNullOrWhiteSpace(settings.Value.JobsApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", settings.Value.JobsApiKey.Trim());
        }
        using var response = await client.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode == 409)
        {
            // A durable refresh can safely attach to the active listing run. The operation
            // still requires a successful crawler timestamp newer than its own baseline.
            return (true, "The TopCV listing crawler is already running; waiting for it.");
        }
        if (!response.IsSuccessStatusCode)
        {
            return (false, $"TopCV crawler trigger returned HTTP {(int)response.StatusCode}.");
        }
        return (true, null);
    }

    private Uri? ResolveCrawlTriggerUrl()
    {
        if (Uri.TryCreate(
                settings.Value.JobsApiCrawlTriggerUrl?.Trim(),
                UriKind.Absolute,
                out var configured))
        {
            return configured;
        }

        var jobsUrl = FirstNonEmpty(settings.Value.JobsApiUrl, settings.Value.ActiveJobsApiUrl);
        if (!Uri.TryCreate(jobsUrl?.Trim(), UriKind.Absolute, out var sourceUri))
        {
            return null;
        }

        return new UriBuilder(sourceUri)
        {
            Path = "/api/crawl/listing/run",
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;
    }

    public async Task<IReadOnlyList<MarketPulseCrawlerFailureDto>> GetCrawlerFailuresAsync(
        string? status,
        int limit,
        CancellationToken cancellationToken)
    {
        var endpoint = ResolveFailedJobsUrl();
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var url = $"{endpoint}{separator}source=topcv&limit={Math.Clamp(limit, 1, 200)}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        using var request = CreateRequest(url);
        var client = httpClientFactory.CreateClient("market-pulse");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<CrawlerFailureEnvelope>(
            JsonOptions,
            cancellationToken);
        return MapCrawlerFailures(envelope);
    }

    public async Task<int> GetOpenCrawlerFailureCountAsync(CancellationToken cancellationToken)
    {
        var openTask = GetCrawlerFailureCountAsync("open", cancellationToken);
        var queuedTask = GetCrawlerFailureCountAsync("retry_queued", cancellationToken);
        var retryingTask = GetCrawlerFailureCountAsync("retrying", cancellationToken);
        await Task.WhenAll(openTask, queuedTask, retryingTask);
        var openCount = await openTask;
        var queuedCount = await queuedTask;
        var retryingCount = await retryingTask;
        return openCount + queuedCount + retryingCount;
    }

    public async Task<IReadOnlyList<MarketPulseCrawlerFailureDto>> UpdateCrawlerFailuresAsync(
        IReadOnlyCollection<long> failureIds,
        string action,
        CancellationToken cancellationToken)
    {
        var normalizedAction = action.Trim().ToLowerInvariant();
        if (normalizedAction is not "retry" and not "ignore")
        {
            throw new ArgumentOutOfRangeException(nameof(action), "Crawler failure action must be retry or ignore.");
        }
        var ids = failureIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        using var request = CreateRequest(
            $"{ResolveFailedJobsUrl()}/{normalizedAction}",
            HttpMethod.Post);
        request.Content = JsonContent.Create(new { failed_item_ids = ids });
        var client = httpClientFactory.CreateClient("market-pulse");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<CrawlerFailureEnvelope>(
            JsonOptions,
            cancellationToken);
        return MapCrawlerFailures(envelope);
    }

    private async Task<int> GetCrawlerFailureCountAsync(
        string status,
        CancellationToken cancellationToken)
    {
        var endpoint = ResolveFailedJobsUrl();
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var url = $"{endpoint}{separator}source=topcv&limit=1&status={Uri.EscapeDataString(status)}";
        using var request = CreateRequest(url);
        var client = httpClientFactory.CreateClient("market-pulse");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<CrawlerFailureEnvelope>(
            JsonOptions,
            cancellationToken);
        return Math.Max(0, envelope?.Pagination?.Total ?? envelope?.Data?.Count ?? 0);
    }

    public Task<JobsApiFetchResult> FetchAsync(
        string url,
        CancellationToken cancellationToken) =>
        FetchAsync(url, JobsApiFetchOptions.Default, cancellationToken);

    public Task<TopCvImportBatch> FetchImportBatchAsync(
        MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken) =>
        FetchImportBatchAsync(request, TopCvJobScope.Active, cancellationToken);

    public async Task<TopCvImportBatch> FetchImportBatchAsync(
        MarketPulseRefreshRequestDto? request,
        TopCvJobScope scope,
        CancellationToken cancellationToken)
    {
        var configuredUrl = FirstNonEmpty(settings.Value.JobsApiUrl, settings.Value.ActiveJobsApiUrl);
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return TopCvImportBatch.Failure(
                JobsApiFetchStatus.InvalidContract,
                "MarketPulse:JobsApiUrl must point to the TopCV Jobs API.");
        }

        var maxItems = Math.Clamp(
            request?.JobsApiMaxItems ?? settings.Value.JobsApiMaxItems,
            1,
            50_000);
        var fetch = await FetchAsync(
            configuredUrl,
            new JobsApiFetchOptions(
                maxItems,
                request?.JobsApiPageSize ?? settings.Value.JobsApiPageSize,
                request?.JobsApiMaxPages ?? settings.Value.JobsApiMaxPages,
                scope),
            cancellationToken);

        if (fetch.Status is not JobsApiFetchStatus.Success and not JobsApiFetchStatus.Empty)
        {
            return TopCvImportBatch.FromFetch(fetch, []);
        }

        var postings = fetch.Jobs
            .Select(ToJobMarketPosting)
            .Select(ToScrapedPosting)
            .ToList();
        return TopCvImportBatch.FromFetch(fetch, postings);
    }

    public async Task<JobsApiFetchResult> FetchAsync(
        string url,
        JobsApiFetchOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
        {
            return InvalidContract("Jobs API URL must be a non-empty absolute URL.");
        }

        var normalizedOptions = options.Normalize();
        var jobs = new List<JobsApiJobDto>();
        int? total = null;
        DateTimeOffset? generatedAt = null;
        DateTimeOffset? latestSuccessfulCrawlAt = null;
        DateTimeOffset? historyCoverageStart = null;
        var isSourceComplete = false;

        var firstPage = await FetchPageAsync(
            WithPaging(url, page: 1, pageSize: normalizedOptions.PageSize, normalizedOptions.Scope),
            cancellationToken);
        if (firstPage.Status != JobsApiFetchStatus.Success)
        {
            return firstPage.ToFetchResult();
        }
        if (firstPage.Page != 1 || firstPage.PageSize != normalizedOptions.PageSize)
        {
            return InvalidContract(
                $"Jobs API returned page={firstPage.Page}, pageSize={firstPage.PageSize}; expected page=1, pageSize={normalizedOptions.PageSize}.");
        }

        total = firstPage.Total;
        generatedAt = firstPage.GeneratedAt;
        latestSuccessfulCrawlAt = firstPage.LatestSuccessfulCrawlAt;
        historyCoverageStart = firstPage.HistoryCoverageStart;
        isSourceComplete = firstPage.IsSourceComplete;
        jobs.AddRange(firstPage.Jobs);

        var totalPages = ResolveTotalPages(firstPage, normalizedOptions);
        var pageLimit = Math.Min(totalPages, normalizedOptions.MaxPages);

        for (var page = 2; page <= pageLimit && jobs.Count < normalizedOptions.MaxItems; page++)
        {
            var nextPage = await FetchPageAsync(
                WithPaging(url, page, normalizedOptions.PageSize, normalizedOptions.Scope),
                cancellationToken);
            if (nextPage.Status != JobsApiFetchStatus.Success)
            {
                return nextPage.ToFetchResult(
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.Page != page || nextPage.PageSize != firstPage.PageSize)
            {
                return InvalidContract(
                    $"Jobs API returned page={nextPage.Page}, pageSize={nextPage.PageSize}; expected page={page}, pageSize={firstPage.PageSize}.",
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.Total != total)
            {
                return InvalidContract(
                    $"Jobs API pagination total changed from {total} to {nextPage.Total} during one fetch.",
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.LatestSuccessfulCrawlAt != latestSuccessfulCrawlAt)
            {
                return InvalidContract(
                    "Jobs API latestSuccessfulCrawlAt changed during one paginated fetch.",
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.HistoryCoverageStart != historyCoverageStart)
            {
                return InvalidContract(
                    "Jobs API historyCoverageStart changed during one paginated fetch.",
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.IsSourceComplete != isSourceComplete)
            {
                return InvalidContract(
                    "Jobs API isSourceComplete changed during one paginated fetch.",
                    total,
                    jobs.Count,
                    generatedAt,
                    latestSuccessfulCrawlAt);
            }

            if (nextPage.Jobs.Count == 0)
            {
                break;
            }

            jobs.AddRange(nextPage.Jobs);
            await DelayBetweenRequestsAsync(cancellationToken);
        }

        var selectedJobs = jobs
            .Where(x => normalizedOptions.Scope switch
            {
                TopCvJobScope.Active => x.IsActive != false,
                TopCvJobScope.Inactive => x.IsActive == false,
                _ => true
            })
            .Take(normalizedOptions.MaxItems)
            .ToList();
        var fetchedCount = selectedJobs.Count;
        if (selectedJobs.Any(job => string.IsNullOrWhiteSpace(job.Url)))
        {
            return InvalidContract(
                "Jobs API returned a job without a URL; lifecycle-safe import cannot silently omit it.",
                total,
                fetchedCount,
                generatedAt,
                latestSuccessfulCrawlAt);
        }
        var stableIdentities = selectedJobs
            .Select(ResolveStableIdentity)
            .ToList();
        if (stableIdentities.Any(string.IsNullOrWhiteSpace))
        {
            return InvalidContract(
                "Jobs API returned a job without id, source_job_id, or URL; lifecycle-safe import requires a stable identity.",
                total,
                fetchedCount,
                generatedAt,
                latestSuccessfulCrawlAt);
        }
        var distinctIdentityCount = stableIdentities
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (distinctIdentityCount != fetchedCount)
        {
            return InvalidContract(
                $"Jobs API pagination returned {fetchedCount - distinctIdentityCount} duplicate stable job identities.",
                total,
                fetchedCount,
                generatedAt,
                latestSuccessfulCrawlAt);
        }
        var isCompleteSync = total.HasValue && distinctIdentityCount >= total.Value &&
            isSourceComplete;
        var freshness = ValidateFreshness(generatedAt, latestSuccessfulCrawlAt);

        if (freshness.Status == JobsApiFetchStatus.InvalidContract)
        {
            return InvalidContract(
                freshness.ErrorMessage ?? "Jobs API freshness metadata is invalid.",
                total,
                fetchedCount,
                generatedAt,
                latestSuccessfulCrawlAt);
        }

        if (freshness.Status == JobsApiFetchStatus.StaleSource &&
            settings.Value.JobsApiFailOnStaleSource)
        {
            return new JobsApiFetchResult
            {
                Status = JobsApiFetchStatus.StaleSource,
                Jobs = selectedJobs,
                Total = total,
                FetchedCount = fetchedCount,
                GeneratedAt = generatedAt,
                LatestSuccessfulCrawlAt = latestSuccessfulCrawlAt,
                HistoryCoverageStart = historyCoverageStart,
                ErrorMessage = freshness.ErrorMessage,
                IsCompleteSync = isCompleteSync,
                IsSourceComplete = isSourceComplete,
                IsSourceFresh = false
            };
        }

        if (freshness.Status == JobsApiFetchStatus.StaleSource)
        {
            logger.LogWarning("{FreshnessWarning}", freshness.ErrorMessage);
        }

        return new JobsApiFetchResult
        {
            Status = fetchedCount == 0 ? JobsApiFetchStatus.Empty : JobsApiFetchStatus.Success,
            Jobs = selectedJobs,
            Total = total,
            FetchedCount = fetchedCount,
            GeneratedAt = generatedAt,
            LatestSuccessfulCrawlAt = latestSuccessfulCrawlAt,
            HistoryCoverageStart = historyCoverageStart,
            ErrorMessage = freshness.Status == JobsApiFetchStatus.StaleSource
                ? freshness.ErrorMessage
                : null,
            IsCompleteSync = isCompleteSync,
            IsSourceComplete = isSourceComplete,
            IsSourceFresh = freshness.Status == JobsApiFetchStatus.Success
        };
    }

    private async Task<JobsApiPageFetchResult> FetchPageAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        var retryMax = Math.Clamp(settings.Value.RetryMax, 1, 6);
        var lastStatus = JobsApiFetchStatus.HttpError;
        string? lastError = null;

        for (var attempt = 1; attempt <= retryMax; attempt++)
        {
            using var request = CreateRequest(url);

            try
            {
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastStatus = JobsApiFetchStatus.HttpError;
                    lastError = $"Jobs API returned HTTP {(int)response.StatusCode} ({response.StatusCode}) for {url}.";

                    if (!IsRetryable(response.StatusCode) || attempt == retryMax)
                    {
                        return JobsApiPageFetchResult.Failure(lastStatus, lastError);
                    }

                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                return await ParsePageAsync(response, url, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastStatus = JobsApiFetchStatus.Timeout;
                lastError = $"Jobs API timed out for {url}: {ex.Message}";
            }
            catch (HttpRequestException ex)
            {
                lastStatus = JobsApiFetchStatus.HttpError;
                lastError = $"Jobs API request failed for {url}: {ex.Message}";
            }

            if (attempt < retryMax)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        logger.LogWarning("{JobsApiError}", lastError);
        return JobsApiPageFetchResult.Failure(
            lastStatus,
            lastError ?? $"Jobs API request failed for {url}.");
    }

    private async Task<JobsApiPageFetchResult> ParsePageAsync(
        HttpResponseMessage response,
        string url,
        CancellationToken cancellationToken)
    {
        JobsApiEnvelope? envelope;

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            envelope = await JsonSerializer.DeserializeAsync<JobsApiEnvelope>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API returned invalid JSON for {url}: {ex.Message}");
        }

        if (envelope is null)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API returned an empty JSON document for {url}.");
        }

        if (envelope.Ok != true)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API contract requires ok=true for {url}.");
        }

        if (envelope.Data is null)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API contract requires a non-null data array for {url}.");
        }

        var invalidProvider = envelope.Data.FirstOrDefault(job =>
            (!string.IsNullOrWhiteSpace(job.Source) &&
             !string.Equals(job.Source.Trim(), Provider, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(job.Id) &&
             job.Id.Contains(':', StringComparison.Ordinal) &&
             !job.Id.StartsWith(Provider + ":", StringComparison.OrdinalIgnoreCase)));
        if (invalidProvider is not null)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"TopCV Jobs API returned unsupported provider '{invalidProvider.Source ?? invalidProvider.Id}'.");
        }

        if (envelope.Pagination?.Total is null || envelope.Pagination.Total < 0)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API contract requires pagination.total to be a non-negative integer for {url}.");
        }

        if (envelope.Pagination.Page is null || envelope.Pagination.Page <= 0 ||
            envelope.Pagination.PageSize is null || envelope.Pagination.PageSize <= 0)
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"Jobs API contract requires positive pagination.page and pagination.pageSize values for {url}.");
        }

        if (!TryParseMetadata(
                envelope.Meta,
                out var generatedAt,
                out var latestSuccessfulCrawlAt,
                out var historyCoverageStart,
                out var isSourceComplete,
                out var metadataError))
        {
            return JobsApiPageFetchResult.Failure(
                JobsApiFetchStatus.InvalidContract,
                $"{metadataError} URL: {url}.");
        }

        return JobsApiPageFetchResult.Success(
            envelope.Pagination.Total.Value,
            envelope.Pagination.Page,
            envelope.Pagination.PageSize,
            envelope.Pagination.TotalPages,
            generatedAt,
            latestSuccessfulCrawlAt,
            historyCoverageStart,
            isSourceComplete,
            envelope.Data);
    }

    private bool TryParseMetadata(
        JobsApiMeta? meta,
        out DateTimeOffset? generatedAt,
        out DateTimeOffset? latestSuccessfulCrawlAt,
        out DateTimeOffset? historyCoverageStart,
        out bool isSourceComplete,
        out string? error)
    {
        generatedAt = null;
        latestSuccessfulCrawlAt = null;
        historyCoverageStart = null;
        isSourceComplete = false;
        error = null;

        if (meta is null)
        {
            if (settings.Value.JobsApiRequireFreshCrawlMetadata)
            {
                error = "Jobs API contract requires meta freshness information.";
                return false;
            }

            return true;
        }

        if (!TryParseDateTimeOffset(meta.GeneratedAt, "meta.generatedAt", out generatedAt, out error))
        {
            return false;
        }

        if (!TryParseDateTimeOffset(
                meta.LatestSuccessfulCrawlAt,
                "meta.latestSuccessfulCrawlAt",
                out var latestCompleteCrawlAt,
                out error))
        {
            return false;
        }

        if (!TryParseDateTimeOffset(
                meta.LatestDataCrawlAt,
                "meta.latestDataCrawlAt",
                out var latestDataCrawlAt,
                out error))
        {
            return false;
        }

        latestSuccessfulCrawlAt = latestDataCrawlAt ?? latestCompleteCrawlAt;
        isSourceComplete = meta.IsSourceComplete ??
            (latestCompleteCrawlAt.HasValue &&
             (!latestDataCrawlAt.HasValue || latestDataCrawlAt == latestCompleteCrawlAt));

        if (!TryParseDateTimeOffset(
                meta.HistoryCoverageStart,
                "meta.historyCoverageStart",
                out historyCoverageStart,
                out error))
        {
            return false;
        }

        if (settings.Value.JobsApiRequireFreshCrawlMetadata &&
            (!generatedAt.HasValue || !latestSuccessfulCrawlAt.HasValue))
        {
            error = "Jobs API contract requires valid meta.generatedAt and meta.latestSuccessfulCrawlAt values.";
            return false;
        }

        return true;
    }

    private FreshnessValidationResult ValidateFreshness(
        DateTimeOffset? generatedAt,
        DateTimeOffset? latestSuccessfulCrawlAt)
    {
        if (settings.Value.JobsApiRequireFreshCrawlMetadata &&
            (!generatedAt.HasValue || !latestSuccessfulCrawlAt.HasValue))
        {
            return new FreshnessValidationResult(
                JobsApiFetchStatus.InvalidContract,
                "Jobs API freshness metadata is required but missing.");
        }

        if (!latestSuccessfulCrawlAt.HasValue)
        {
            return new FreshnessValidationResult(JobsApiFetchStatus.Success, null);
        }

        var now = DateTimeOffset.UtcNow;
        if (latestSuccessfulCrawlAt.Value > now.AddMinutes(5))
        {
            return new FreshnessValidationResult(
                JobsApiFetchStatus.InvalidContract,
                "Jobs API latestSuccessfulCrawlAt is unexpectedly in the future.");
        }

        var maxFreshnessHours = Math.Clamp(settings.Value.JobsApiMaxFreshnessHours, 1, 720);
        var age = now - latestSuccessfulCrawlAt.Value;
        if (age > TimeSpan.FromHours(maxFreshnessHours))
        {
            return new FreshnessValidationResult(
                JobsApiFetchStatus.StaleSource,
                $"Python crawler data is stale: latest successful crawl is {age.TotalHours:F1} hours old; maximum is {maxFreshnessHours} hours.");
        }

        return new FreshnessValidationResult(JobsApiFetchStatus.Success, null);
    }

    private HttpRequestMessage CreateRequest(string url, HttpMethod? method = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(settings.Value.JobsApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", settings.Value.JobsApiKey.Trim());
        }

        return request;
    }

    private string ResolveFailedJobsUrl()
    {
        var configured = FirstNonEmpty(settings.Value.JobsApiUrl, settings.Value.ActiveJobsApiUrl);
        if (string.IsNullOrWhiteSpace(configured) ||
            !Uri.TryCreate(configured, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("MarketPulse:JobsApiUrl is not configured.");
        }

        var builder = new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty };
        var path = builder.Path.TrimEnd('/');
        const string apiMarker = "/api/v1/";
        var apiIndex = path.IndexOf(apiMarker, StringComparison.OrdinalIgnoreCase);
        builder.Path = apiIndex >= 0
            ? path[..(apiIndex + apiMarker.Length)] + "failed-jobs"
            : path.EndsWith("/jobs", StringComparison.OrdinalIgnoreCase)
                ? path[..^5] + "/failed-jobs"
                : path + "/failed-jobs";
        return builder.Uri.ToString();
    }

    private static IReadOnlyList<MarketPulseCrawlerFailureDto> MapCrawlerFailures(
        CrawlerFailureEnvelope? envelope)
    {
        if (envelope?.Ok != true || envelope.Data is null)
        {
            throw new InvalidOperationException("TopCV failed-jobs API returned an invalid contract.");
        }
        var invalidSource = envelope.Data.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.Source) &&
            !string.Equals(item.Source, Provider, StringComparison.OrdinalIgnoreCase));
        if (invalidSource is not null)
        {
            throw new InvalidOperationException("TopCV failed-jobs API returned a non-TopCV item.");
        }

        return envelope.Data.Select(item => new MarketPulseCrawlerFailureDto
        {
            FailureId = $"crawler:{item.Id}",
            Stage = item.Stage ?? "unknown",
            ErrorCode = item.ErrorCode ?? "UNKNOWN",
            ErrorMessage = item.ErrorMessage ?? "TopCV crawler failure",
            Status = item.Status ?? "open",
            RetryCount = item.RetryCount,
            CreatedAt = item.CreatedAt,
            LastRetryAt = item.LastRetryAt,
            Actionable = item.Id > 0
        }).ToList();
    }

    internal static JobMarketPosting ToJobMarketPosting(JobsApiJobDto job)
    {
        var category = FirstNonEmpty(job.CategoryNormalized, job.Category);
        var location = FirstNonEmpty(job.PrimaryCity, job.Location);
        var updatedAt = ParseDateTime(FirstNonEmpty(job.UpdatedAt, job.LastSeenAt, job.FirstSeenAt));

        return new JobMarketPosting
        {
            Id = Clean(job.Id),
            SourceJobId = Clean(FirstNonEmpty(job.SourceJobId, StripSourcePrefix(job.Id))),
            Source = Provider,
            Title = Clean(job.Title),
            Company = Clean(job.Company),
            Category = Clean(category),
            Location = Clean(location),
            Salary = Clean(job.Salary),
            SalaryRaw = Clean(job.SalaryRaw),
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            SalaryCurrency = Clean(job.SalaryCurrency),
            SalaryIsNegotiable = job.SalaryIsNegotiable,
            Experience = Clean(job.Experience),
            ExperienceRaw = Clean(job.ExperienceRaw),
            ExperienceMinYears = job.ExperienceMinYears,
            ExperienceMaxYears = job.ExperienceMaxYears,
            PostedOn = ParseDateOnly(job.PostDate),
            PostedOnText = Clean(job.PostDateText),
            PostDateConfidence = MarketPulseBusinessTime.NormalizePostDateConfidence(
                job.PostDateConfidence),
            PostDateLowerBound = ParseDateOnly(job.PostDateLowerBound),
            PostDateUpperBound = ParseDateOnly(job.PostDateUpperBound),
            PostDateObservedOn = ParseDateOnly(job.PostDateObservedOn),
            UpdatedAt = updatedAt,
            DetailStatus = Clean(job.DetailStatus),
            DetailLastSuccessAt = ParseDateTime(job.DetailLastSuccessAt),
            Url = Clean(job.Url),
            IsActive = job.IsActive != false,
            Requirements = CleanList(job.Requirements),
            Specialties = CleanList(job.Specialties),
            Benefits = CleanList(job.Benefits),
            Skills = CleanList(job.SkillsNormalized)
        };
    }

    private static int ResolveTotalPages(
        JobsApiPageFetchResult page,
        JobsApiFetchOptions options)
    {
        if (page.TotalPages.GetValueOrDefault() > 0)
        {
            return page.TotalPages!.Value;
        }

        if (page.Total.GetValueOrDefault() <= page.Jobs.Count || page.Jobs.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)page.Total!.Value / options.PageSize);
    }

    private static bool TryParseDateTimeOffset(
        string? value,
        string fieldName,
        out DateTimeOffset? parsed,
        out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(
                value.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            error = $"Jobs API {fieldName} is not a valid ISO timestamp.";
            return false;
        }

        parsed = timestamp;
        return true;
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;

    private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var backoffBaseMs = Math.Clamp(settings.Value.BackoffBaseMs, 250, 30_000);
        var delayMs = Math.Min(
            backoffBaseMs * (int)Math.Pow(2, attempt - 1) + Random.Shared.Next(100, 900),
            60_000);
        await Task.Delay(delayMs, cancellationToken);
    }

    private static Task DelayBetweenRequestsAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static string WithPaging(string url, int page, int pageSize, TopCvJobScope scope)
    {
        var uri = new Uri(url);
        var builder = new UriBuilder(uri);
        var query = ParseQuery(builder.Query);
        if (builder.Path.EndsWith("/jobs/active", StringComparison.OrdinalIgnoreCase))
        {
            builder.Path = builder.Path[..^"/active".Length];
        }
        query.Remove("active");
        query["page"] = page.ToString(CultureInfo.InvariantCulture);
        query["page_size"] = pageSize.ToString(CultureInfo.InvariantCulture);
        query["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        query["scope"] = scope.ToString().ToLowerInvariant();
        builder.Query = string.Join(
            '&',
            query.Select(x =>
                $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

        return builder.Uri.ToString();
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cleanQuery = query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return values;
        }

        foreach (var pair in cleanQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var date)
            ? date
            : null;
    }

    private static IReadOnlyList<string> CleanList(IEnumerable<string>? values) =>
        values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static string? ResolveStableIdentity(JobsApiJobDto job)
    {
        var identity = FirstNonEmpty(job.Id, job.SourceJobId, job.Url);
        return string.IsNullOrWhiteSpace(identity) ? null : identity.Trim().ToLowerInvariant();
    }

    private static string? StripSourcePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var separatorIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 && separatorIndex < trimmed.Length - 1
            ? trimmed[(separatorIndex + 1)..]
            : trimmed;
    }

    private static ScrapedJobPosting ToScrapedPosting(JobMarketPosting job)
    {
        var parts = new[]
        {
            job.Title,
            job.Company,
            job.Category,
            job.Salary,
            job.Experience,
            job.Location,
            string.Join(' ', job.Skills),
            string.Join(' ', job.Requirements),
            string.Join(' ', job.Specialties),
            string.Join(' ', job.Benefits)
        };

        return new ScrapedJobPosting(
            job.Title?.Trim() ?? "Untitled IT job",
            job.Company,
            job.Location,
            job.Url!,
            string.Join(' ', parts.Where(value => !string.IsNullOrWhiteSpace(value))),
            job.PostedOn?.ToDateTime(TimeOnly.MinValue),
            null,
            job.SourceJobId,
            job.Category,
            job.Salary,
            job.Experience,
            job.PostedOnText,
            job.UpdatedAt,
            job.Requirements,
            job.Specialties,
            job.Benefits,
            job.Skills,
            job.SalaryRaw,
            job.SalaryMin,
            job.SalaryMax,
            job.SalaryCurrency,
            job.SalaryIsNegotiable,
            job.ExperienceRaw,
            job.ExperienceMinYears,
            job.ExperienceMaxYears,
            job.PostDateConfidence,
            job.PostDateLowerBound?.ToDateTime(TimeOnly.MinValue),
            job.PostDateUpperBound?.ToDateTime(TimeOnly.MinValue),
            job.PostDateObservedOn?.ToDateTime(TimeOnly.MinValue),
            job.DetailStatus,
            job.DetailLastSuccessAt,
            job.IsActive,
            job.Id);
    }

    private static JobsApiFetchResult InvalidContract(
        string error,
        int? total = null,
        int fetchedCount = 0,
        DateTimeOffset? generatedAt = null,
        DateTimeOffset? latestSuccessfulCrawlAt = null) =>
        new()
        {
            Status = JobsApiFetchStatus.InvalidContract,
            Total = total,
            FetchedCount = fetchedCount,
            GeneratedAt = generatedAt,
            LatestSuccessfulCrawlAt = latestSuccessfulCrawlAt,
            ErrorMessage = error,
            IsCompleteSync = false,
            IsSourceFresh = false
        };
}

public enum TopCvJobScope
{
    Active,
    Inactive,
    All
}

public sealed record JobsApiFetchOptions(
    int MaxItems,
    int PageSize,
    int MaxPages,
    TopCvJobScope Scope = TopCvJobScope.Active)
{
    public static JobsApiFetchOptions Default { get; } = new(50_000, 100, 500);

    public JobsApiFetchOptions Normalize() => new(
        Math.Clamp(MaxItems, 1, 50_000),
        Math.Clamp(PageSize, 1, 500),
        Math.Clamp(MaxPages, 1, 500),
        Scope);
}

internal sealed class JobsApiPageFetchResult
{
    public JobsApiFetchStatus Status { get; init; }

    public int? Total { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public int? TotalPages { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }

    public DateTimeOffset? LatestSuccessfulCrawlAt { get; init; }

    public DateTimeOffset? HistoryCoverageStart { get; init; }

    public bool IsSourceComplete { get; init; }

    public IReadOnlyList<JobsApiJobDto> Jobs { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public static JobsApiPageFetchResult Success(
        int total,
        int? page,
        int? pageSize,
        int? totalPages,
        DateTimeOffset? generatedAt,
        DateTimeOffset? latestSuccessfulCrawlAt,
        DateTimeOffset? historyCoverageStart,
        bool isSourceComplete,
        IReadOnlyList<JobsApiJobDto> jobs) =>
        new()
        {
            Status = JobsApiFetchStatus.Success,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            GeneratedAt = generatedAt,
            LatestSuccessfulCrawlAt = latestSuccessfulCrawlAt,
            HistoryCoverageStart = historyCoverageStart,
            IsSourceComplete = isSourceComplete,
            Jobs = jobs
        };

    public static JobsApiPageFetchResult Failure(
        JobsApiFetchStatus status,
        string error) =>
        new()
        {
            Status = status,
            ErrorMessage = error
        };

    public JobsApiFetchResult ToFetchResult(
        int? total = null,
        int fetchedCount = 0,
        DateTimeOffset? generatedAt = null,
        DateTimeOffset? latestSuccessfulCrawlAt = null) =>
        new()
        {
            Status = Status,
            Total = total ?? Total,
            FetchedCount = fetchedCount,
            GeneratedAt = generatedAt ?? GeneratedAt,
            LatestSuccessfulCrawlAt = latestSuccessfulCrawlAt ?? LatestSuccessfulCrawlAt,
            ErrorMessage = ErrorMessage,
            IsCompleteSync = false,
            IsSourceComplete = IsSourceComplete,
            IsSourceFresh = false
        };
}

internal sealed record FreshnessValidationResult(
    JobsApiFetchStatus Status,
    string? ErrorMessage);

internal sealed class JobsApiEnvelope
{
    [JsonPropertyName("ok")]
    public bool? Ok { get; set; }

    [JsonPropertyName("pagination")]
    public JobsApiPagination? Pagination { get; set; }

    [JsonPropertyName("meta")]
    public JobsApiMeta? Meta { get; set; }

    [JsonPropertyName("data")]
    public List<JobsApiJobDto>? Data { get; set; }
}

internal sealed class JobsApiPagination
{
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int? TotalPages { get; set; }
}

internal sealed class JobsApiMeta
{
    [JsonPropertyName("generatedAt")]
    public string? GeneratedAt { get; set; }

    [JsonPropertyName("latestSuccessfulCrawlAt")]
    public string? LatestSuccessfulCrawlAt { get; set; }

    [JsonPropertyName("latestDataCrawlAt")]
    public string? LatestDataCrawlAt { get; set; }

    [JsonPropertyName("isSourceComplete")]
    public bool? IsSourceComplete { get; set; }

    [JsonPropertyName("historyCoverageStart")]
    public string? HistoryCoverageStart { get; set; }
}

public sealed class JobsApiJobDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("source_job_id")]
    public string? SourceJobId { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("salary")]
    public string? Salary { get; set; }

    [JsonPropertyName("salary_raw")]
    public string? SalaryRaw { get; set; }

    [JsonPropertyName("salary_min")]
    public long? SalaryMin { get; set; }

    [JsonPropertyName("salary_max")]
    public long? SalaryMax { get; set; }

    [JsonPropertyName("salary_currency")]
    public string? SalaryCurrency { get; set; }

    [JsonPropertyName("salary_is_negotiable")]
    public bool? SalaryIsNegotiable { get; set; }

    [JsonPropertyName("experience")]
    public string? Experience { get; set; }

    [JsonPropertyName("experience_raw")]
    public string? ExperienceRaw { get; set; }

    [JsonPropertyName("experience_min_years")]
    public int? ExperienceMinYears { get; set; }

    [JsonPropertyName("experience_max_years")]
    public int? ExperienceMaxYears { get; set; }

    [JsonPropertyName("post_date")]
    public string? PostDate { get; set; }

    [JsonPropertyName("post_date_text")]
    public string? PostDateText { get; set; }

    [JsonPropertyName("post_date_confidence")]
    public string? PostDateConfidence { get; set; }

    [JsonPropertyName("post_date_lower_bound")]
    public string? PostDateLowerBound { get; set; }

    [JsonPropertyName("post_date_upper_bound")]
    public string? PostDateUpperBound { get; set; }

    [JsonPropertyName("post_date_observed_on")]
    public string? PostDateObservedOn { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("category_normalized")]
    public string? CategoryNormalized { get; set; }

    [JsonPropertyName("benefits")]
    public List<string>? Benefits { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("primary_city")]
    public string? PrimaryCity { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("requirements")]
    public List<string>? Requirements { get; set; }

    [JsonPropertyName("specialties")]
    public List<string>? Specialties { get; set; }

    [JsonPropertyName("skills_normalized")]
    public List<string>? SkillsNormalized { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("first_seen_at")]
    public string? FirstSeenAt { get; set; }

    [JsonPropertyName("last_seen_at")]
    public string? LastSeenAt { get; set; }

    [JsonPropertyName("detail_status")]
    public string? DetailStatus { get; set; }

    [JsonPropertyName("detail_last_success_at")]
    public string? DetailLastSuccessAt { get; set; }
}

internal sealed class CrawlerFailureEnvelope
{
    public bool? Ok { get; set; }

    public List<CrawlerFailureItem>? Data { get; set; }

    public CrawlerFailurePagination? Pagination { get; set; }
}

internal sealed class CrawlerFailurePagination
{
    public int Total { get; set; }
}

internal sealed class CrawlerFailureItem
{
    public long Id { get; set; }

    public string? Source { get; set; }

    public string? Stage { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Status { get; set; }

    public int RetryCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastRetryAt { get; set; }
}

public sealed record ScrapedJobPosting(
    string Title,
    string? CompanyName,
    string? Location,
    string Url,
    string Description,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    string? SourceJobId = null,
    string? Category = null,
    string? Salary = null,
    string? Experience = null,
    string? PostDateText = null,
    DateTime? SourceUpdatedAt = null,
    IReadOnlyList<string>? Requirements = null,
    IReadOnlyList<string>? Specialties = null,
    IReadOnlyList<string>? Benefits = null,
    IReadOnlyList<string>? Skills = null,
    string? SalaryRaw = null,
    long? SalaryMin = null,
    long? SalaryMax = null,
    string? SalaryCurrency = null,
    bool? SalaryIsNegotiable = null,
    string? ExperienceRaw = null,
    int? ExperienceMinYears = null,
    int? ExperienceMaxYears = null,
    string? PostDateConfidence = null,
    DateTime? PostDateLowerBound = null,
    DateTime? PostDateUpperBound = null,
    DateTime? PostDateObservedOn = null,
    string? DetailStatus = null,
    DateTime? DetailLastSuccessAt = null,
    bool IsActive = true,
    string? ExternalId = null);

public sealed class TopCvImportBatch
{
    public JobsApiFetchStatus Status { get; init; }

    public IReadOnlyList<ScrapedJobPosting> Postings { get; init; } = [];

    public int? Total { get; init; }

    public int FetchedCount { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }

    public DateTimeOffset? LatestSuccessfulCrawlAt { get; init; }

    public DateTimeOffset? HistoryCoverageStart { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsCompleteSync { get; init; }

    public bool IsSourceComplete { get; init; }

    public bool IsSourceFresh { get; init; }

    public static TopCvImportBatch FromFetch(
        JobsApiFetchResult fetch,
        IReadOnlyList<ScrapedJobPosting> postings) => new()
    {
        Status = fetch.Status,
        Postings = postings,
        Total = fetch.Total,
        FetchedCount = fetch.FetchedCount,
        GeneratedAt = fetch.GeneratedAt,
        LatestSuccessfulCrawlAt = fetch.LatestSuccessfulCrawlAt,
        HistoryCoverageStart = fetch.HistoryCoverageStart,
        ErrorMessage = fetch.ErrorMessage,
        IsCompleteSync = fetch.IsCompleteSync,
        IsSourceComplete = fetch.IsSourceComplete,
        IsSourceFresh = fetch.IsSourceFresh
    };

    public static TopCvImportBatch Failure(JobsApiFetchStatus status, string error) => new()
    {
        Status = status,
        ErrorMessage = error,
        IsCompleteSync = false,
        IsSourceFresh = false
    };
}
