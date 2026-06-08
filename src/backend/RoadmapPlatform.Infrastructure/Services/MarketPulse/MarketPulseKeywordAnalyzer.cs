using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseKeywordAnalyzer
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

    public IReadOnlyList<KeywordDefinition> BuildDefinitions(IReadOnlyCollection<string> configuredKeywords)
    {
        var specs = configuredKeywords.Count > 0
            ? configuredKeywords
            : DefaultKeywordSpecs;

        return specs
            .Select(ParseSpec)
            .GroupBy(x => x.Slug)
            .Select(g => g.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    public IReadOnlyList<KeywordFrequency> Analyze(
        IReadOnlyCollection<string> documents,
        IReadOnlyCollection<KeywordDefinition> definitions)
    {
        return definitions
            .Select(definition =>
            {
                var mentionCount = 0;
                var postingCount = 0;

                foreach (var document in documents)
                {
                    var documentMentions = definition.Aliases.Sum(alias => CountOccurrences(document, alias));

                    if (documentMentions > 0)
                    {
                        postingCount++;
                        mentionCount += documentMentions;
                    }
                }

                return new KeywordFrequency(
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

    private static KeywordDefinition ParseSpec(string spec)
    {
        var aliases = spec
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var name = aliases.FirstOrDefault() ?? spec.Trim();
        return new KeywordDefinition(name, Slugify(name), aliases.Count > 0 ? aliases : [name]);
    }

    private static int CountOccurrences(string input, string keyword)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keyword))
        {
            return 0;
        }

        var count = 0;
        var index = 0;

        while (index < input.Length)
        {
            var matchIndex = input.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                break;
            }

            var beforeIsBoundary = matchIndex == 0 || IsBoundary(input[matchIndex - 1]);
            var afterIndex = matchIndex + keyword.Length;
            var afterIsBoundary = afterIndex >= input.Length || IsBoundary(input[afterIndex]);

            if (beforeIsBoundary && afterIsBoundary)
            {
                count++;
            }

            index = matchIndex + keyword.Length;
        }

        return count;
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

public sealed record KeywordDefinition(string Name, string Slug, IReadOnlyList<string> Aliases);

public sealed record KeywordFrequency(string SkillName, string SkillSlug, int MentionCount, int PostingCount);
