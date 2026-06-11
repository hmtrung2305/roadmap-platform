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
        var result = await jobsApiClient.FetchAsync(source.SearchUrlTemplate, cancellationToken);
        var maxPostings = Math.Max(1, settings.MaxPostingsPerSource);

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
                x.Benefits))
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
            using var response = await client.GetAsync(searchUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Market pulse source {SourceName} returned {StatusCode} for {SearchUrl}.",
                    source.Name,
                    response.StatusCode,
                    searchUrl);
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
        }

        var postings = new List<ScrapedJobPosting>();

        foreach (var url in links.Take(Math.Max(1, settings.MaxPostingsPerSource)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var posting = await ScrapePostingAsync(source.Name, url, cancellationToken);

                if (posting != null)
                {
                    postings.Add(posting);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Failed to scrape job posting {Url}.", url);
            }

            if (settings.RequestDelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.RequestDelaySeconds), cancellationToken);
            }
        }

        return postings;
    }

    private async Task<ScrapedJobPosting?> ScrapePostingAsync(
        string sourceName,
        string url,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("market-pulse");
        using var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
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
    IReadOnlyList<string>? Benefits = null);
