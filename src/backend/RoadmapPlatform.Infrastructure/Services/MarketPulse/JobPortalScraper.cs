using System.Net;
using System.Text.RegularExpressions;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public interface IJobPortalScraper
{
    Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(
        MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken);
}

public sealed class JobPortalScraper(
    IHttpClientFactory httpClientFactory,
    JobsApiClient jobsApiClient,
    IOptions<MarketPulseSettings> options,
    ILogger<JobPortalScraper> logger) : IJobPortalScraper
{
    private const string HtmlSourceKind = "Html";
    private const string JobsApiSourceKind = "JobsApi";

    private static readonly Regex HrefRegex = new(
        "href\\s*=\\s*[\"'](?<href>[^\"'#]+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitleRegex = new(
        "<title[^>]*>(?<title>.*?)</title>|<h1[^>]*>(?<title>.*?)</h1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(CancellationToken cancellationToken) =>
        ScrapeAsync(null, cancellationToken);

    public async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(
        MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var effectiveSettings = ResolveEffectiveSettings(settings, request);
        var postings = new List<ScrapedJobPosting>();
        var enabledSources = ResolveEnabledSources(settings).ToList();

        if (enabledSources.Count == 0)
        {
            logger.LogWarning(
                "No Market Pulse sources are enabled or configured. Set MarketPulse__Sources__0__SearchUrlTemplate, " +
                "MarketPulse__ActiveJobsApiUrl, or a JobsApi BaseUrl that points to the Jobs API origin.");
            return postings;
        }

        foreach (var source in enabledSources)
        {
            try
            {
                var sourcePostings = source.Kind switch
                {
                    JobsApiSourceKind => await ScrapeJobsApiSourceAsync(source, effectiveSettings, cancellationToken),
                    HtmlSourceKind => await ScrapeHtmlSourceAsync(source, settings, effectiveSettings, cancellationToken),
                    _ => await ScrapeHtmlSourceAsync(source, settings, effectiveSettings, cancellationToken)
                };

                postings.AddRange(sourcePostings);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to scrape market pulse source {SourceName}.", source.Name);
            }
        }

        return postings;
    }

    private IEnumerable<MarketPulseSourceSettings> ResolveEnabledSources(MarketPulseSettings settings)
    {
        var resolvedAnySource = false;

        foreach (var source in settings.Sources.Where(x => x.Enabled))
        {
            var resolvedSource = ResolveSource(source, settings);
            if (resolvedSource is null)
            {
                logger.LogWarning(
                    "Skipping Market Pulse source {SourceName} because no search URL is configured.",
                    string.IsNullOrWhiteSpace(source.Name) ? "(unnamed)" : source.Name);
                continue;
            }

            resolvedAnySource = true;
            yield return resolvedSource;
        }

        if (!resolvedAnySource && !string.IsNullOrWhiteSpace(settings.ActiveJobsApiUrl))
        {
            yield return new MarketPulseSourceSettings
            {
                Name = "topcv",
                Kind = JobsApiSourceKind,
                BaseUrl = ResolveBaseUrl(settings.ActiveJobsApiUrl),
                SearchUrlTemplate = settings.ActiveJobsApiUrl.Trim(),
                Enabled = true,
                DetailUrlContains = []
            };
        }
    }

    private static MarketPulseSourceSettings? ResolveSource(
        MarketPulseSourceSettings source,
        MarketPulseSettings settings)
    {
        var searchUrlTemplate = source.SearchUrlTemplate;
        if (IsJobsApiSource(source) && string.IsNullOrWhiteSpace(searchUrlTemplate))
        {
            searchUrlTemplate = FirstNonEmpty(
                settings.ActiveJobsApiUrl,
                ResolveJobsApiActiveUrl(source.BaseUrl));
        }

        if (string.IsNullOrWhiteSpace(searchUrlTemplate))
        {
            return null;
        }

        return new MarketPulseSourceSettings
        {
            Name = string.IsNullOrWhiteSpace(source.Name) ? "topcv" : source.Name.Trim(),
            Kind = string.IsNullOrWhiteSpace(source.Kind) ? HtmlSourceKind : source.Kind.Trim(),
            BaseUrl = FirstNonEmpty(source.BaseUrl, ResolveBaseUrl(searchUrlTemplate)) ?? string.Empty,
            SearchUrlTemplate = searchUrlTemplate.Trim(),
            Enabled = true,
            DetailUrlContains = source.DetailUrlContains ?? []
        };
    }

    private static bool IsJobsApiSource(MarketPulseSourceSettings source) =>
        string.Equals(source.Kind, JobsApiSourceKind, StringComparison.OrdinalIgnoreCase);

    private static string? ResolveJobsApiActiveUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var trimmed = baseUrl.Trim();
        if (trimmed.Contains("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return Uri.TryCreate(trimmed.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            ? new Uri(uri, "api/jobs/active").ToString()
            : null;
    }

    private static string ResolveBaseUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        return $"{uri.Scheme}://{uri.Authority}";
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeJobsApiSourceAsync(
        MarketPulseSourceSettings source,
        EffectiveScrapeSettings effectiveSettings,
        CancellationToken cancellationToken)
    {
        var maxPostings = effectiveSettings.MaxPostingsPerSource;
        var result = await jobsApiClient.FetchAsync(
            source.SearchUrlTemplate,
            new JobsApiFetchOptions(
                maxPostings,
                effectiveSettings.JobsApiPageSize,
                effectiveSettings.JobsApiMaxPages),
            cancellationToken);

        return result.Jobs
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Url))
            .Take(maxPostings)
            .Select(x => new ScrapedJobPosting(
                string.IsNullOrWhiteSpace(x.Source) ? source.Name : x.Source.Trim(),
                TrimTo(x.Title?.Trim() ?? string.Empty, 250) is { Length: > 0 } title
                    ? title
                    : "Untitled IT job",
                TrimOptional(x.Company, 160),
                TrimOptional(x.Location, 160),
                x.Url!.Trim(),
                BuildJobsApiDescription(x),
                x.PostedOn?.ToDateTime(TimeOnly.MinValue),
                null,
                x.Id,
                x.Category,
                x.Salary,
                x.Experience,
                x.PostedOnText,
                x.UpdatedAt,
                x.Requirements,
                x.Specialties,
                x.Benefits,
                x.Skills))
            .ToList();
    }

    private async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeHtmlSourceAsync(
        MarketPulseSourceSettings source,
        MarketPulseSettings settings,
        EffectiveScrapeSettings effectiveSettings,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        var links = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPages = effectiveSettings.MaxPagesPerSource;

        for (var page = 1; page <= maxPages; page++)
        {
            var searchUrl = BuildSearchUrl(source.SearchUrlTemplate, settings.SearchKeyword, page);
            using var response = await SendGetWithRetryAsync(client, searchUrl, settings, cancellationToken);

            if (response is null)
            {
                logger.LogWarning(
                    "Market pulse source {SourceName} failed after retries for {SearchUrl}.",
                    source.Name,
                    searchUrl);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Market pulse source {SourceName} returned {StatusCode} for {SearchUrl}.",
                    source.Name,
                    response.StatusCode,
                    searchUrl);

                if (IsSoftBlockStatus(response.StatusCode))
                {
                    logger.LogWarning(
                        "Stopping source {SourceName} softly after protected/block response {StatusCode}.",
                        source.Name,
                        response.StatusCode);
                    break;
                }

                continue;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            foreach (var link in ExtractDetailLinks(html, source))
            {
                links.Add(link);

                if (links.Count >= effectiveSettings.MaxPostingsPerSource)
                {
                    break;
                }
            }

            if (links.Count >= effectiveSettings.MaxPostingsPerSource)
            {
                break;
            }

            await DelayBetweenRequestsAsync(settings, cancellationToken);
        }

        var postings = new List<ScrapedJobPosting>();

        foreach (var url in links.Take(effectiveSettings.MaxPostingsPerSource))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var posting = await ScrapePostingAsync(source.Name, url, settings, cancellationToken);

                if (posting != null)
                {
                    postings.Add(posting);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Failed to scrape job posting {Url}.", url);
            }

            await DelayBetweenRequestsAsync(settings, cancellationToken);
        }

        return postings;
    }

    private async Task<ScrapedJobPosting?> ScrapePostingAsync(
        string sourceName,
        string url,
        MarketPulseSettings settings,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        using var response = await SendGetWithRetryAsync(client, url, settings, cancellationToken);

        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var text = NormalizeText(StripHtml(html));
        var title = ExtractTitle(html);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new ScrapedJobPosting(
            sourceName,
            title.Length > 0 ? title : "Untitled IT job",
            null,
            null,
            url,
            text,
            null,
            null);
    }

    private async Task<HttpResponseMessage?> SendGetWithRetryAsync(
        HttpClient client,
        string url,
        MarketPulseSettings settings,
        CancellationToken cancellationToken)
    {
        var retryMax = Math.Clamp(settings.RetryMax, 1, 6);
        var backoffBaseMs = Math.Clamp(settings.BackoffBaseMs, 250, 30_000);

        for (var attempt = 1; attempt <= retryMax; attempt++)
        {
            try
            {
                var response = await client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (response.IsSuccessStatusCode || IsSoftBlockStatus(response.StatusCode))
                {
                    return response;
                }

                if ((int)response.StatusCode < 500)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogDebug(ex, "Market pulse request failed for {Url} on attempt {Attempt}.", url, attempt);
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

    private static async Task DelayBetweenRequestsAsync(
        MarketPulseSettings settings,
        CancellationToken cancellationToken)
    {
        var minMs = settings.DelayMinMs > 0
            ? settings.DelayMinMs
            : Math.Max(0, settings.RequestDelaySeconds * 1000);
        var maxMs = settings.DelayMaxMs > 0
            ? settings.DelayMaxMs
            : minMs;

        if (maxMs < minMs)
        {
            (minMs, maxMs) = (maxMs, minMs);
        }

        var delayMs = Math.Clamp(Random.Shared.Next(minMs, maxMs + 1), 0, 120_000);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }
    }

    private static bool IsSoftBlockStatus(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Forbidden or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.ServiceUnavailable;

    private static EffectiveScrapeSettings ResolveEffectiveSettings(
        MarketPulseSettings settings,
        MarketPulseRefreshRequestDto? request) =>
        new(
            ClampPositive(request?.MaxPagesPerSource ?? settings.MaxPagesPerSource, 1, 100),
            ClampPositive(request?.MaxPostingsPerSource ?? settings.MaxPostingsPerSource, 1, 5_000),
            ClampPositive(request?.JobsApiPageSize ?? settings.JobsApiPageSize, 1, 500),
            ClampPositive(request?.JobsApiMaxPages ?? settings.JobsApiMaxPages, 1, 100));

    private static int ClampPositive(int value, int min, int max) =>
        Math.Clamp(value, min, max);

    private static string BuildSearchUrl(string template, string keyword, int page)
    {
        return template
            .Replace("{keyword}", Uri.EscapeDataString(keyword), StringComparison.OrdinalIgnoreCase)
            .Replace("{page}", page.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{1}", page.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ExtractDetailLinks(string html, MarketPulseSourceSettings source)
    {
        var baseUri = new Uri(source.BaseUrl);
        var patterns = source.DetailUrlContains ?? [];

        return HrefRegex
            .Matches(html)
            .Select(match => match.Groups["href"].Value)
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .Select(href => TryResolveHttpUrl(baseUri, WebUtility.HtmlDecode(href)))
            .OfType<string>()
            .Where(url => patterns.Length == 0 || patterns.Any(pattern => url.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ExtractTitle(string html)
    {
        var match = TitleRegex.Match(html);

        if (!match.Success)
        {
            return string.Empty;
        }

        return TrimTo(NormalizeText(StripHtml(match.Groups["title"].Value)), 250);
    }

    private static string? TryResolveHttpUrl(Uri baseUri, string href)
    {
        if (!Uri.TryCreate(baseUri, href, out var uri))
        {
            return null;
        }

        return uri.Scheme is "http" or "https"
            ? uri.ToString()
            : null;
    }

    private static string StripHtml(string html)
    {
        var withoutScripts = Regex.Replace(
            html,
            "<(script|style)[^>]*>.*?</\\1>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var withoutTags = Regex.Replace(withoutScripts, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(withoutTags);
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(value, "\\s+", " ").Trim();
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TrimTo(value.Trim(), maxLength);
    }

    private static string BuildJobsApiDescription(JobMarketPosting job)
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

        return NormalizeText(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }
}

public sealed record ScrapedJobPosting(
    string SourceName,
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
    IReadOnlyList<string>? Skills = null);

internal sealed record EffectiveScrapeSettings(
    int MaxPagesPerSource,
    int MaxPostingsPerSource,
    int JobsApiPageSize,
    int JobsApiMaxPages);
