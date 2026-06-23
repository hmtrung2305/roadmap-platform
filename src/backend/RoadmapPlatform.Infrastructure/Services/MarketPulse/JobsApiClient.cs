using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class JobsApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketPulseSettings> settings,
    ILogger<JobsApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<JobsApiResult> FetchAsync(
        string url,
        CancellationToken cancellationToken) =>
        FetchAsync(url, JobsApiFetchOptions.Default, cancellationToken);

    public async Task<JobsApiResult> FetchAsync(
        string url,
        JobsApiFetchOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Jobs API URL is empty.");
            return JobsApiResult.Empty;
        }

        var normalizedOptions = options.Normalize();
        var firstUrl = WithPaging(url, page: 1, pageSize: normalizedOptions.PageSize);

        try
        {
            var firstPage = await FetchPageAsync(firstUrl, cancellationToken);
            if (firstPage == null)
            {
                return JobsApiResult.Empty;
            }

            var jobs = firstPage.Jobs.ToList();
            var total = firstPage.Total > 0 ? firstPage.Total : jobs.Count;
            var totalPages = ResolveTotalPages(firstPage, normalizedOptions);
            var pageLimit = Math.Min(totalPages, normalizedOptions.MaxPages);

            for (var page = 2; page <= pageLimit && jobs.Count < normalizedOptions.MaxItems; page++)
            {
                var nextUrl = WithPaging(url, page, normalizedOptions.PageSize);
                var nextPage = await FetchPageAsync(nextUrl, cancellationToken);

                if (nextPage == null || nextPage.Jobs.Count == 0)
                {
                    break;
                }

                jobs.AddRange(nextPage.Jobs);
                await DelayBetweenRequestsAsync(cancellationToken);
            }

            return new JobsApiResult(
                total,
                jobs
                    .Where(x => x.IsActive)
                    .Take(normalizedOptions.MaxItems)
                    .ToList());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
        {
            logger.LogWarning(ex, "Could not read Jobs API data from {Url}.", url);
            return JobsApiResult.Empty;
        }
    }

    private async Task<JobsApiPageResult?> FetchPageAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await SendWithRetryAsync(client, request, cancellationToken);

        if (response is null)
        {
            logger.LogWarning("Jobs API failed after retries for {Url}.", url);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Jobs API returned {StatusCode} for {Url}.",
                response.StatusCode,
                url);
            return null;
        }

        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<JobsApiEnvelope>(
                JsonOptions,
                cancellationToken);
            var jobs = envelope?.Data
                .Select(ToJobMarketPosting)
                .ToList() ?? [];

            return new JobsApiPageResult(
                envelope?.Pagination?.Total ?? envelope?.Total ?? jobs.Count,
                envelope?.Pagination?.Page ?? envelope?.Page,
                envelope?.Pagination?.PageSize ?? envelope?.PageSize,
                envelope?.Pagination?.TotalPages ?? envelope?.TotalPages,
                jobs);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not read Jobs API data from {Url}.", url);
            return null;
        }
    }

    private async Task<HttpResponseMessage?> SendWithRetryAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var retryMax = Math.Clamp(settings.Value.RetryMax, 1, 6);
        var backoffBaseMs = Math.Clamp(settings.Value.BackoffBaseMs, 250, 30_000);

        for (var attempt = 1; attempt <= retryMax; attempt++)
        {
            using var attemptRequest = CloneRequest(request);

            try
            {
                var response = await client.SendAsync(
                    attemptRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (response.IsSuccessStatusCode ||
                    response.StatusCode is System.Net.HttpStatusCode.Forbidden or
                        System.Net.HttpStatusCode.TooManyRequests or
                        System.Net.HttpStatusCode.ServiceUnavailable ||
                    (int)response.StatusCode < 500)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogDebug(ex, "Jobs API request failed for {Url} on attempt {Attempt}.", request.RequestUri, attempt);
            }

            if (attempt < retryMax)
            {
                var delayMs = Math.Min(
                    backoffBaseMs * (int)Math.Pow(2, attempt - 1) + Random.Shared.Next(100, 900),
                    60_000);
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return null;
    }

    private async Task DelayBetweenRequestsAsync(CancellationToken cancellationToken)
    {
        var minMs = settings.Value.DelayMinMs;
        var maxMs = settings.Value.DelayMaxMs;

        if (maxMs < minMs)
        {
            (minMs, maxMs) = (maxMs, minMs);
        }

        var delayMs = Math.Clamp(Random.Shared.Next(Math.Max(0, minMs), Math.Max(0, maxMs) + 1), 0, 120_000);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private static JobMarketPosting ToJobMarketPosting(JobsApiJob job)
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

    private static int ResolveTotalPages(JobsApiPageResult page, JobsApiFetchOptions options)
    {
        if (page.TotalPages.GetValueOrDefault() > 0)
        {
            return page.TotalPages!.Value;
        }

        if (page.Total <= page.Jobs.Count || page.Jobs.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)page.Total / options.PageSize);
    }

    private static string WithPaging(string url, int page, int pageSize)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

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

    private static IReadOnlyList<string> CleanList(IEnumerable<string>? values)
    {
        return values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
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
}

public sealed record JobsApiResult(int Total, IReadOnlyList<JobMarketPosting> Jobs)
{
    public static JobsApiResult Empty { get; } = new(0, []);
}

public sealed record JobsApiFetchOptions(int MaxItems, int PageSize, int MaxPages)
{
    public static JobsApiFetchOptions Default { get; } = new(160, 100, 2);

    public JobsApiFetchOptions Normalize() => new(
        Math.Clamp(MaxItems, 1, 2_000),
        Math.Clamp(PageSize, 1, 500),
        Math.Clamp(MaxPages, 1, 100));
}

internal sealed record JobsApiPageResult(
    int Total,
    int? Page,
    int? PageSize,
    int? TotalPages,
    IReadOnlyList<JobMarketPosting> Jobs);

internal sealed class JobsApiEnvelope
{
    [JsonPropertyName("ok")]
    public bool? Ok { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    [JsonPropertyName("total_pages")]
    public int? TotalPages { get; set; }

    [JsonPropertyName("pagination")]
    public JobsApiPagination? Pagination { get; set; }

    [JsonPropertyName("data")]
    public List<JobsApiJob> Data { get; set; } = [];
}

internal sealed class JobsApiPagination
{
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int? TotalPages { get; set; }
}

internal sealed class JobsApiJob
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

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("category_normalized")]
    public string? CategoryNormalized { get; set; }

    [JsonPropertyName("benefits")]
    public List<string> Benefits { get; set; } = [];

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

    [JsonPropertyName("post_date_text")]
    public string? PostDateText { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("requirements")]
    public List<string> Requirements { get; set; } = [];

    [JsonPropertyName("specialties")]
    public List<string> Specialties { get; set; } = [];

    [JsonPropertyName("skills_normalized")]
    public List<string> SkillsNormalized { get; set; } = [];

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("first_seen_at")]
    public string? FirstSeenAt { get; set; }

    [JsonPropertyName("last_seen_at")]
    public string? LastSeenAt { get; set; }
}
