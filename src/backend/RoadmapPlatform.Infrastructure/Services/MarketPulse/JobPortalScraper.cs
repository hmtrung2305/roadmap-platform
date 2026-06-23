using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public interface IJobPortalScraper
{
    Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(CancellationToken cancellationToken);
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

    public async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var postings = new List<ScrapedJobPosting>();
        var enabledSources = settings.Sources
            .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.SearchUrlTemplate))
            .ToList();

        foreach (var source in enabledSources)
        {
            try
            {
                var sourcePostings = source.Kind switch
                {
                    JobsApiSourceKind => await ScrapeJobsApiSourceAsync(source, settings, cancellationToken),
                    HtmlSourceKind => await ScrapeHtmlSourceAsync(source, settings, cancellationToken),
                    _ => await ScrapeHtmlSourceAsync(source, settings, cancellationToken)
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

    private async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeJobsApiSourceAsync(
        MarketPulseSourceSettings source,
        MarketPulseSettings settings,
        CancellationToken cancellationToken)
    {
        var maxPostings = Math.Max(1, settings.MaxPostingsPerSource);
        var result = await jobsApiClient.FetchAsync(
            source.SearchUrlTemplate,
            new JobsApiFetchOptions(
                maxPostings,
                Math.Max(1, settings.JobsApiPageSize),
                Math.Max(1, settings.JobsApiMaxPages)),
            cancellationToken);

        return result.Jobs
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Url))
            .Take(maxPostings)
            .Select(x => new ScrapedJobPosting(
                source.Name,
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
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        var links = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPages = Math.Max(1, settings.MaxPagesPerSource);

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

                if (links.Count >= settings.MaxPostingsPerSource)
                {
                    break;
                }
            }

            if (links.Count >= settings.MaxPostingsPerSource)
            {
                break;
            }

            await DelayBetweenRequestsAsync(settings, cancellationToken);
        }

        var postings = new List<ScrapedJobPosting>();

        foreach (var url in links.Take(Math.Max(1, settings.MaxPostingsPerSource)))
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
