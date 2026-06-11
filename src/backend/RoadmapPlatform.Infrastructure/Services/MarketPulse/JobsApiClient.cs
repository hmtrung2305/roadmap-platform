using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RoadmapPlatform.Application.Models.MarketPulse;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class JobsApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<JobsApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<JobsApiResult> FetchAsync(
        string url,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Jobs API URL is empty.");
            return JobsApiResult.Empty;
        }

        try
        {
            var client = httpClientFactory.CreateClient("market-pulse");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Jobs API returned {StatusCode} for {Url}.",
                    response.StatusCode,
                    url);
                return JobsApiResult.Empty;
            }

            var envelope = await response.Content.ReadFromJsonAsync<JobsApiEnvelope>(
                JsonOptions,
                cancellationToken);
            var jobs = envelope?.Data
                .Select(ToJobMarketPosting)
                .ToList() ?? [];

            return new JobsApiResult(envelope?.Total ?? jobs.Count, jobs);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Could not read Jobs API data from {Url}.", url);
            return JobsApiResult.Empty;
        }
    }

    private static JobMarketPosting ToJobMarketPosting(JobsApiJob job)
    {
        return new JobMarketPosting
        {
            Id = Clean(job.Id),
            Title = Clean(job.Title),
            Company = Clean(job.Company),
            Category = Clean(job.Category),
            Location = Clean(job.Location),
            Salary = Clean(job.Salary),
            Experience = Clean(job.Experience),
            PostedOn = ParseDateOnly(job.PostDate),
            PostedOnText = Clean(job.PostDateText),
            UpdatedAt = ParseDateTime(job.UpdatedAt),
            Url = Clean(job.Url),
            IsActive = job.IsActive != false,
            Requirements = CleanList(job.Requirements),
            Specialties = CleanList(job.Specialties),
            Benefits = CleanList(job.Benefits)
        };
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
}

public sealed record JobsApiResult(int Total, IReadOnlyList<JobMarketPosting> Jobs)
{
    public static JobsApiResult Empty { get; } = new(0, []);
}

internal sealed class JobsApiEnvelope
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public List<JobsApiJob> Data { get; set; } = [];
}

internal sealed class JobsApiJob
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("salary")]
    public string? Salary { get; set; }

    [JsonPropertyName("experience")]
    public string? Experience { get; set; }

    [JsonPropertyName("post_date")]
    public string? PostDate { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

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

    [JsonPropertyName("post_date_text")]
    public string? PostDateText { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("requirements")]
    public List<string> Requirements { get; set; } = [];

    [JsonPropertyName("specialties")]
    public List<string> Specialties { get; set; } = [];

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
