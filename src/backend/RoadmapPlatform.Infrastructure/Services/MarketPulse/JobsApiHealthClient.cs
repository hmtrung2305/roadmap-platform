using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class JobsApiHealthClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketPulseSettings> options,
    ILogger<JobsApiHealthClient> logger) : IJobsApiHealthService
{
    public async Task<MarketPulseExternalSourceHealthDto> GetHealthAsync(
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;
        var healthUrl = ResolveHealthUrl(options.Value);
        if (healthUrl is null)
        {
            return Unavailable(
                checkedAt,
                "not_configured",
                "Python Jobs API ops health URL is not configured and cannot be derived.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
        if (!string.IsNullOrWhiteSpace(options.Value.JobsApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", options.Value.JobsApiKey.Trim());
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(
            Math.Clamp(options.Value.JobsApiHealthTimeoutSeconds, 2, 60)));

        try
        {
            var client = httpClientFactory.CreateClient("market-pulse");
            using var response = await client.SendAsync(request, timeoutSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                var status = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "unauthorized",
                    HttpStatusCode.TooManyRequests => "rate_limited",
                    _ => "http_error"
                };
                return Unavailable(
                    checkedAt,
                    status,
                    $"Python ops health returned HTTP {(int)response.StatusCode}.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<JobsApiOpsHealthEnvelope>(
                cancellationToken: timeoutSource.Token);
            if (envelope is not { Ok: true, Data: not null })
            {
                return Unavailable(
                    checkedAt,
                    "invalid_contract",
                    "Python ops health returned an invalid response contract.");
            }

            return Map(envelope.Data, checkedAt, options.Value.JobsApiMaxFreshnessHours);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(checkedAt, "timeout", "Python ops health request timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Python Jobs API ops health request failed.");
            return Unavailable(checkedAt, "unavailable", "Python Jobs API is unavailable.");
        }
        catch (NotSupportedException exception)
        {
            logger.LogWarning(exception, "Python Jobs API ops health content type is invalid.");
            return Unavailable(
                checkedAt,
                "invalid_contract",
                "Python ops health response content type is invalid.");
        }
        catch (System.Text.Json.JsonException exception)
        {
            logger.LogWarning(exception, "Python Jobs API ops health JSON is invalid.");
            return Unavailable(
                checkedAt,
                "invalid_contract",
                "Python ops health response JSON is invalid.");
        }
    }

    private static MarketPulseExternalSourceHealthDto Map(
        JobsApiOpsHealthData data,
        DateTime checkedAt,
        int maxFreshnessHours)
    {
        var hoursSinceData = data.Freshness?.HoursSinceDataListing ??
            data.Freshness?.HoursSinceSuccessfulListing;
        var latestDataAt = data.Freshness?.LatestDataListingRunAt ??
            data.Freshness?.LatestSuccessfulListingRunAt;
        var isStale = !hoursSinceData.HasValue ||
            hoursSinceData.Value > Math.Max(1, maxFreshnessHours);
        var listingStatus = data.LatestListingRun?.Status;
        var usablePartial = string.Equals(
                listingStatus,
                "partial_success",
                StringComparison.OrdinalIgnoreCase) &&
            data.LatestListingRun?.UniqueJobsSeen > 0;
        var isBlocked = !usablePartial &&
            (string.Equals(listingStatus, "blocked", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(listingStatus, "layout_changed", StringComparison.OrdinalIgnoreCase) ||
             data.LatestListingRun?.PagesBlocked > 0);
        var status = string.IsNullOrWhiteSpace(data.PipelineStatus)
            ? "unknown"
            : data.PipelineStatus.Trim().ToLowerInvariant();
        if (isStale && status == "healthy")
        {
            status = "stale";
        }

        return new MarketPulseExternalSourceHealthDto
        {
            IsAvailable = true,
            Status = status,
            IsStale = isStale,
            IsBlocked = isBlocked,
            CheckedAt = checkedAt,
            GeneratedAt = data.GeneratedAt,
            LatestSuccessfulCrawlAt = latestDataAt,
            HoursSinceSuccessfulCrawl = hoursSinceData,
            LatestListingStatus = listingStatus,
            LatestListingStartedAt = data.LatestListingRun?.StartedAt,
            LatestListingFinishedAt = data.LatestListingRun?.FinishedAt,
            LatestListingJobsSeen = data.LatestListingRun?.UniqueJobsSeen ?? 0,
            PagesBlocked = data.LatestListingRun?.PagesBlocked ?? 0,
            PagesFailed = data.LatestListingRun?.PagesFailed ?? 0,
            ActiveJobs = data.DataQuality?.ActiveJobs ?? 0,
            NewJobsToday = data.DataQuality?.NewJobsToday ?? 0,
            DetailCompletionRate = data.DataQuality?.DetailCompletionRate ?? 0,
            Warnings = data.Warnings ?? []
        };
    }

    private static MarketPulseExternalSourceHealthDto Unavailable(
        DateTime checkedAt,
        string status,
        string message) =>
        new()
        {
            IsAvailable = false,
            Status = status,
            IsStale = true,
            CheckedAt = checkedAt,
            ErrorMessage = message,
            Warnings = [message]
        };

    private static Uri? ResolveHealthUrl(MarketPulseSettings settings)
    {
        if (Uri.TryCreate(settings.JobsApiOpsHealthUrl?.Trim(), UriKind.Absolute, out var configured))
        {
            return configured;
        }

        var candidate = string.IsNullOrWhiteSpace(settings.JobsApiUrl)
            ? settings.ActiveJobsApiUrl
            : settings.JobsApiUrl;

        if (!Uri.TryCreate(candidate?.Trim(), UriKind.Absolute, out var sourceUri))
        {
            return null;
        }

        var builder = new UriBuilder(sourceUri)
        {
            Path = "/api/v1/ops/health-summary",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }
}

internal sealed class JobsApiOpsHealthEnvelope
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("data")]
    public JobsApiOpsHealthData? Data { get; set; }
}

internal sealed class JobsApiOpsHealthData
{
    [JsonPropertyName("pipeline_status")]
    public string? PipelineStatus { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }

    [JsonPropertyName("latest_listing_run")]
    public JobsApiOpsRun? LatestListingRun { get; set; }

    [JsonPropertyName("data_quality")]
    public JobsApiOpsDataQuality? DataQuality { get; set; }

    [JsonPropertyName("freshness")]
    public JobsApiOpsFreshness? Freshness { get; set; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string>? Warnings { get; set; }
}

internal sealed class JobsApiOpsRun
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [JsonPropertyName("pages_blocked")]
    public int? PagesBlocked { get; set; }

    [JsonPropertyName("pages_failed")]
    public int? PagesFailed { get; set; }

    [JsonPropertyName("unique_jobs_seen")]
    public int? UniqueJobsSeen { get; set; }
}

internal sealed class JobsApiOpsDataQuality
{
    [JsonPropertyName("active_jobs")]
    public int ActiveJobs { get; set; }

    [JsonPropertyName("new_jobs_today")]
    public int NewJobsToday { get; set; }

    [JsonPropertyName("detail_completion_rate")]
    public decimal DetailCompletionRate { get; set; }
}

internal sealed class JobsApiOpsFreshness
{
    [JsonPropertyName("latest_successful_listing_run_at")]
    public DateTime? LatestSuccessfulListingRunAt { get; set; }

    [JsonPropertyName("hours_since_successful_listing")]
    public double? HoursSinceSuccessfulListing { get; set; }

    [JsonPropertyName("latest_data_listing_run_at")]
    public DateTime? LatestDataListingRunAt { get; set; }

    [JsonPropertyName("hours_since_data_listing")]
    public double? HoursSinceDataListing { get; set; }

    [JsonPropertyName("is_source_complete")]
    public bool IsSourceComplete { get; set; }
}
