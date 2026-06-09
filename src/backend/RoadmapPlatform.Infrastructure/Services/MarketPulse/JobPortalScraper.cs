using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public interface IJobPortalScraper
{
    Task<IReadOnlyList<ScrapedJobPosting>> ScrapeAsync(CancellationToken cancellationToken);
}

public sealed class JobPortalScraper(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketPulseSettings> options,
    ILogger<JobPortalScraper> logger) : IJobPortalScraper
{
    private const string HtmlSourceKind = "Html";
    private const string TopCvPythonSourceKind = "TopCvPython";

    private static readonly Regex HrefRegex = new(
        "href\\s*=\\s*[\"'](?<href>[^\"'#]+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitleRegex = new(
        "<title[^>]*>(?<title>.*?)</title>|<h1[^>]*>(?<title>.*?)</h1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
                    TopCvPythonSourceKind => await ScrapeTopCvWithPythonAsync(source, settings, cancellationToken),
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

    private async Task<IReadOnlyList<ScrapedJobPosting>> ScrapeTopCvWithPythonAsync(
        MarketPulseSourceSettings source,
        MarketPulseSettings settings,
        CancellationToken cancellationToken)
    {
        var scriptPath = ResolveScriptPath(settings.PythonScriptPath);

        if (scriptPath == null)
        {
            logger.LogWarning(
                "TopCV Python scraper script was not found. Configured path: {PythonScriptPath}",
                settings.PythonScriptPath);
            return [];
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(30, settings.RequestTimeoutSeconds * Math.Max(1, settings.MaxPostingsPerSource))));

        foreach (var command in BuildPythonCommands(settings.PythonExecutablePath))
        {
            using var process = TryStartTopCvProcess(
                command,
                scriptPath,
                source,
                settings);

            if (process == null)
            {
                continue;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                logger.LogInformation("TopCV Python scraper diagnostics: {Diagnostics}", TrimLog(stderr, 4000));
            }

            if (process.ExitCode != 0)
            {
                logger.LogWarning(
                    "TopCV Python scraper exited with code {ExitCode} using {PythonCommand}.",
                    process.ExitCode,
                    command.DisplayName);
                return [];
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                return [];
            }

            var postings = JsonSerializer.Deserialize<List<ScrapedJobPosting>>(stdout, JsonOptions) ?? [];
            logger.LogInformation(
                "TopCV Python scraper returned {PostingCount} normalized postings using {PythonCommand}.",
                postings.Count,
                command.DisplayName);
            return postings;
        }

        logger.LogWarning(
            "Could not start a Python process for TopCV. Configure MarketPulse:PythonExecutablePath with an absolute Python executable path.");
        return [];
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

    private static string? ResolveScriptPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
            Path.Combine(Directory.GetCurrentDirectory(), "RoadmapPlatform.Api", configuredPath)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private Process? TryStartTopCvProcess(
        PythonCommand command,
        string scriptPath,
        MarketPulseSourceSettings source,
        MarketPulseSettings settings)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        processStartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        processStartInfo.Environment["PYTHONUTF8"] = "1";

        foreach (var argument in command.PrefixArguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        processStartInfo.ArgumentList.Add(scriptPath);
        processStartInfo.ArgumentList.Add("--source-name");
        processStartInfo.ArgumentList.Add(source.Name);
        processStartInfo.ArgumentList.Add("--base-url");
        processStartInfo.ArgumentList.Add(source.BaseUrl);
        processStartInfo.ArgumentList.Add("--search-url-template");
        processStartInfo.ArgumentList.Add(source.SearchUrlTemplate);
        processStartInfo.ArgumentList.Add("--keyword");
        processStartInfo.ArgumentList.Add(settings.SearchKeyword);
        processStartInfo.ArgumentList.Add("--pages");
        processStartInfo.ArgumentList.Add(Math.Max(1, settings.MaxPagesPerSource).ToString());
        processStartInfo.ArgumentList.Add("--limit");
        processStartInfo.ArgumentList.Add(Math.Max(1, settings.MaxPostingsPerSource).ToString());
        processStartInfo.ArgumentList.Add("--delay");
        processStartInfo.ArgumentList.Add(Math.Max(0, settings.RequestDelaySeconds).ToString());
        processStartInfo.ArgumentList.Add("--timeout");
        processStartInfo.ArgumentList.Add(Math.Max(5, settings.RequestTimeoutSeconds).ToString());

        try
        {
            return Process.Start(processStartInfo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Could not start Python command {PythonCommand}.", command.DisplayName);
            return null;
        }
    }

    private static IReadOnlyList<PythonCommand> BuildPythonCommands(string configuredExecutable)
    {
        var commands = new List<PythonCommand>();

        if (!string.IsNullOrWhiteSpace(configuredExecutable))
        {
            commands.Add(new PythonCommand(configuredExecutable.Trim(), []));
        }

        commands.Add(new PythonCommand("python", []));
        commands.Add(new PythonCommand("py", ["-3"]));
        commands.Add(new PythonCommand("python3", []));

        return commands
            .GroupBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup for a timed-out ingestion process.
        }
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

    private static string TrimLog(string value, int maxLength)
    {
        var normalized = NormalizeText(value);
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
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
    DateTime? ExpiresAt);

internal sealed record PythonCommand(string FileName, IReadOnlyList<string> PrefixArguments)
{
    public string DisplayName => PrefixArguments.Count == 0
        ? FileName
        : $"{FileName} {string.Join(' ', PrefixArguments)}";
}
