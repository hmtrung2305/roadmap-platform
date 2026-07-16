using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public string? ErrorMessage { get; init; }

    public bool IsCompleteSync { get; init; }

    public bool IsSourceFresh { get; init; }
}

public sealed class JobsApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketPulseSettings> settings,
    ILogger<JobsApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<JobsApiFetchResult> FetchAsync(
        string url,
        CancellationToken cancellationToken) =>
        FetchAsync(url, JobsApiFetchOptions.Default, cancellationToken);

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

        var firstPage = await FetchPageAsync(
            WithPaging(url, page: 1, pageSize: normalizedOptions.PageSize),
            cancellationToken);
        if (firstPage.Status != JobsApiFetchStatus.Success)
        {
            return firstPage.ToFetchResult();
        }

        total = firstPage.Total;
        generatedAt = firstPage.GeneratedAt;
        latestSuccessfulCrawlAt = firstPage.LatestSuccessfulCrawlAt;
        jobs.AddRange(firstPage.Jobs);

        var totalPages = ResolveTotalPages(firstPage, normalizedOptions);
        var pageLimit = Math.Min(totalPages, normalizedOptions.MaxPages);

        for (var page = 2; page <= pageLimit && jobs.Count < normalizedOptions.MaxItems; page++)
        {
            var nextPage = await FetchPageAsync(
                WithPaging(url, page, normalizedOptions.PageSize),
                cancellationToken);
            if (nextPage.Status != JobsApiFetchStatus.Success)
            {
                return nextPage.ToFetchResult(
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

            if (nextPage.Jobs.Count == 0)
            {
                break;
            }

            jobs.AddRange(nextPage.Jobs);
            await DelayBetweenRequestsAsync(cancellationToken);
        }

        var selectedJobs = jobs
            .Where(x => x.IsActive != false)
            .Take(normalizedOptions.MaxItems)
            .ToList();
        var fetchedCount = selectedJobs.Count;
        var isCompleteSync = total.HasValue && fetchedCount >= total.Value;
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
                ErrorMessage = freshness.ErrorMessage,
                IsCompleteSync = isCompleteSync,
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
            ErrorMessage = freshness.Status == JobsApiFetchStatus.StaleSource
                ? freshness.ErrorMessage
                : null,
            IsCompleteSync = isCompleteSync,
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
            envelope.Data);
    }

    private bool TryParseMetadata(
        JobsApiMeta? meta,
        out DateTimeOffset? generatedAt,
        out DateTimeOffset? latestSuccessfulCrawlAt,
        out string? error)
    {
        generatedAt = null;
        latestSuccessfulCrawlAt = null;
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
                out latestSuccessfulCrawlAt,
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

    private HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(settings.Value.JobsApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", settings.Value.JobsApiKey.Trim());
        }

        return request;
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
            Source = Clean(job.Source),
            Title = Clean(job.Title),
            Company = Clean(job.Company),
            Category = Clean(category),
            Location = Clean(location),
            Salary = Clean(job.Salary),
            Experience = Clean(job.Experience),
            PostedOn = ParseDateOnly(job.PostDate),
            PostedOnText = Clean(job.PostDateText),
            UpdatedAt = updatedAt,
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

    private async Task DelayBetweenRequestsAsync(CancellationToken cancellationToken)
    {
        var minMs = Math.Clamp(settings.Value.DelayMinMs, 0, 120_000);
        var maxMs = Math.Clamp(settings.Value.DelayMaxMs, 0, 120_000);

        if (maxMs < minMs)
        {
            (minMs, maxMs) = (maxMs, minMs);
        }

        var delayMs = Random.Shared.Next(minMs, maxMs + 1);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }
    }

    private static string WithPaging(string url, int page, int pageSize)
    {
        var uri = new Uri(url);
        var builder = new UriBuilder(uri);
        var query = ParseQuery(builder.Query);
        query["page"] = page.ToString(CultureInfo.InvariantCulture);
        query["page_size"] = pageSize.ToString(CultureInfo.InvariantCulture);
        query["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
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

public sealed record JobsApiFetchOptions(int MaxItems, int PageSize, int MaxPages)
{
    public static JobsApiFetchOptions Default { get; } = new(160, 100, 2);

    public JobsApiFetchOptions Normalize() => new(
        Math.Clamp(MaxItems, 1, 5_000),
        Math.Clamp(PageSize, 1, 500),
        Math.Clamp(MaxPages, 1, 100));
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

    public IReadOnlyList<JobsApiJobDto> Jobs { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public static JobsApiPageFetchResult Success(
        int total,
        int? page,
        int? pageSize,
        int? totalPages,
        DateTimeOffset? generatedAt,
        DateTimeOffset? latestSuccessfulCrawlAt,
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

    [JsonPropertyName("experience")]
    public string? Experience { get; set; }

    [JsonPropertyName("post_date")]
    public string? PostDate { get; set; }

    [JsonPropertyName("post_date_text")]
    public string? PostDateText { get; set; }

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
}
