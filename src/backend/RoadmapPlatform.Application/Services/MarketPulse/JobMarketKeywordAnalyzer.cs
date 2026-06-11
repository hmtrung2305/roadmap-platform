using System.Text.RegularExpressions;
using RoadmapPlatform.Application.Models.MarketPulse;

namespace RoadmapPlatform.Application.Services.MarketPulse;

public sealed class JobMarketKeywordAnalyzer
{
    private static readonly string[] DefaultKeywordSpecs =
    [
        "JavaScript|JS|ECMAScript",
        "TypeScript|TS",
        "React|React.js|ReactJS",
        "Angular|AngularJS",
        "Vue|Vue.js",
        "Node.js|NodeJS|Node",
        "ASP.NET|ASP.NET Core|.NET|C#",
        "Java|Spring|Spring Boot",
        "Python|Django|FastAPI|Flask",
        "Go|Golang",
        "PHP|Laravel",
        "SQL|PostgreSQL|MySQL|SQL Server",
        "NoSQL|MongoDB|Redis",
        "Docker|Kubernetes|K8s",
        "AWS|Amazon Web Services",
        "Azure",
        "Google Cloud|GCP",
        "DevOps|CI/CD|GitHub Actions|Jenkins",
        "AI|Machine Learning|ML|LLM",
        "Data Engineering|Spark|Kafka"
    ];

    public IReadOnlyList<JobMarketKeywordDefinition> BuildDefinitions(
        IReadOnlyCollection<string> configuredKeywords)
    {
        var specs = configuredKeywords.Count > 0
            ? configuredKeywords
            : DefaultKeywordSpecs;

        return specs
            .Select(ParseSpec)
            .Where(x => x.Aliases.Count > 0)
            .GroupBy(x => x.Slug)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    public IReadOnlyList<JobMarketKeywordFrequency> Analyze(
        IReadOnlyCollection<string> documents,
        IReadOnlyCollection<JobMarketKeywordDefinition> definitions)
    {
        return definitions
            .Select(definition =>
            {
                var mentionCount = 0;
                var postingCount = 0;

                foreach (var document in documents)
                {
                    var documentMentions = CountDefinitionMentions(document, definition);

                    if (documentMentions <= 0)
                    {
                        continue;
                    }

                    postingCount++;
                    mentionCount += documentMentions;
                }

                return new JobMarketKeywordFrequency(
                    definition.Name,
                    definition.Slug,
                    mentionCount,
                    postingCount);
            })
            .Where(x => x.MentionCount > 0)
            .OrderByDescending(x => x.MentionCount)
            .ThenBy(x => x.SkillName)
            .ToList();
    }

    private static JobMarketKeywordDefinition ParseSpec(string spec)
    {
        var aliases = spec
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var name = aliases.FirstOrDefault() ?? spec.Trim();
        return new JobMarketKeywordDefinition(name, Slugify(name), aliases.Count > 0 ? aliases : [name]);
    }

    private static int CountDefinitionMentions(
        string document,
        JobMarketKeywordDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            return 0;
        }

        var occupiedRanges = new List<(int Start, int End)>();

        foreach (var alias in definition.Aliases.OrderByDescending(x => x.Length))
        {
            foreach (var range in FindOccurrences(document, alias))
            {
                if (occupiedRanges.Any(existing => RangesOverlap(existing, range)))
                {
                    continue;
                }

                occupiedRanges.Add(range);
            }
        }

        return occupiedRanges.Count;
    }

    private static IEnumerable<(int Start, int End)> FindOccurrences(string input, string keyword)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keyword))
        {
            yield break;
        }

        var index = 0;

        while (index < input.Length)
        {
            var matchIndex = input.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                yield break;
            }

            var afterIndex = matchIndex + keyword.Length;
            var beforeIsBoundary = matchIndex == 0 || IsBoundary(input[matchIndex - 1]);
            var afterIsBoundary = afterIndex >= input.Length || IsBoundary(input[afterIndex]);

            if (beforeIsBoundary && afterIsBoundary)
            {
                yield return (matchIndex, afterIndex);
            }

            index = afterIndex;
        }
    }

    private static bool RangesOverlap((int Start, int End) left, (int Start, int End) right)
    {
        return left.Start < right.End && right.Start < left.End;
    }

    private static bool IsBoundary(char value)
    {
        return !char.IsLetterOrDigit(value);
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 0 ? slug : "skill";
    }
}
