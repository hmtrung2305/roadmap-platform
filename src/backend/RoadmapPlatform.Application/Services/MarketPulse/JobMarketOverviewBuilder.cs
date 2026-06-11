using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Models.MarketPulse;

namespace RoadmapPlatform.Application.Services.MarketPulse;

public sealed class JobMarketOverviewBuilder(JobMarketKeywordAnalyzer keywordAnalyzer)
{
    public MarketPulseOverviewDto Build(
        JobMarketSnapshot snapshot,
        JobMarketOverviewOptions options)
    {
        var days = Math.Clamp(options.Days, 7, 180);
        var activeJobs = Deduplicate(snapshot.ActiveJobs)
            .Where(x => x.IsActive)
            .ToList();
        var todayJobs = Deduplicate(snapshot.TodayJobs)
            .Where(x => x.IsActive)
            .ToList();
        var reportDate = GetLatestPostDate(todayJobs) ??
            GetLatestPostDate(activeJobs) ??
            options.ReferenceDate;
        var previousDate = reportDate.AddDays(-1);

        var definitions = keywordAnalyzer.BuildDefinitions(options.TrackedKeywordSpecs);
        var activeFrequencies = AnalyzePostings(activeJobs, definitions);
        var todayFrequencies = AnalyzePostings(todayJobs, definitions);
        var previousDayFrequencies = AnalyzePostings(
            activeJobs.Where(x => x.PostedOn == previousDate),
            definitions);
        var todayMentionCounts = ToMentionLookup(todayFrequencies);
        var previousMentionCounts = ToMentionLookup(previousDayFrequencies);

        var skills = activeFrequencies
            .Select(x => ToSkillSummary(
                x,
                todayMentionCounts.GetValueOrDefault(x.SkillSlug),
                previousMentionCounts.GetValueOrDefault(x.SkillSlug)))
            .OrderByDescending(x => x.MentionCount)
            .ThenBy(x => x.SkillName)
            .ToList();
        var todaySkills = todayFrequencies
            .Select(x => ToSkillSummary(
                x,
                x.MentionCount,
                previousMentionCounts.GetValueOrDefault(x.SkillSlug)))
            .OrderByDescending(x => x.MentionCount)
            .ThenBy(x => x.SkillName)
            .ToList();
        var visibleSkillSlugs = ResolveVisibleSkillSlugs(
            options.SelectedSkillSlugs,
            skills,
            Math.Max(1, options.MaxVisibleSkills));

        return new MarketPulseOverviewDto
        {
            LastUpdatedAt = GetLatestUpdatedAt(activeJobs.Concat(todayJobs)),
            TotalPostings = Math.Max(snapshot.ActiveTotal, activeJobs.Count),
            ActivePostings = activeJobs.Count,
            TodayPostings = Math.Max(snapshot.TodayTotal, todayJobs.Count),
            StalePostings = 0,
            ExpiredPostings = snapshot.ActiveJobs.Count(x => !x.IsActive),
            SourceCount = CountSources(activeJobs),
            Skills = skills,
            TodaySkills = todaySkills,
            TrendPoints = BuildTrendPoints(activeJobs, definitions, visibleSkillSlugs, days, reportDate),
            CategorySummaries = BuildSegmentSummaries(activeJobs.Select(x => x.Category), activeJobs.Count, options.MaxSegmentCount),
            LocationSummaries = BuildSegmentSummaries(activeJobs.Select(x => x.Location), activeJobs.Count, options.MaxSegmentCount),
            TodayJobs = todayJobs
                .OrderByDescending(GetUpdatedSortValue)
                .ThenByDescending(GetPostDateSortValue)
                .Take(Math.Max(1, options.MaxTodayJobs))
                .Select(ToJobPostingDto)
                .ToList(),
            RecentJobs = activeJobs
                .OrderByDescending(GetPostDateSortValue)
                .ThenByDescending(GetUpdatedSortValue)
                .Take(Math.Max(1, options.MaxRecentJobs))
                .Select(ToJobPostingDto)
                .ToList()
        };
    }

    private IReadOnlyList<JobMarketKeywordFrequency> AnalyzePostings(
        IEnumerable<JobMarketPosting> postings,
        IReadOnlyCollection<JobMarketKeywordDefinition> definitions)
    {
        var documents = postings
            .Select(BuildSearchDocument)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return keywordAnalyzer.Analyze(documents, definitions);
    }

    private IReadOnlyList<MarketTrendPointDto> BuildTrendPoints(
        IReadOnlyCollection<JobMarketPosting> jobs,
        IReadOnlyCollection<JobMarketKeywordDefinition> definitions,
        IReadOnlyCollection<string> visibleSkillSlugs,
        int days,
        DateOnly reportDate)
    {
        if (visibleSkillSlugs.Count == 0)
        {
            return [];
        }

        var definitionBySlug = definitions.ToDictionary(
            x => x.Slug,
            x => x,
            StringComparer.OrdinalIgnoreCase);
        var startDate = reportDate.AddDays(-(days - 1));
        var jobsByDate = jobs
            .Where(x => x.PostedOn.HasValue && x.PostedOn.Value >= startDate && x.PostedOn.Value <= reportDate)
            .GroupBy(x => x.PostedOn!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());
        var points = new List<MarketTrendPointDto>();

        foreach (var date in Enumerable.Range(0, days).Select(offset => startDate.AddDays(offset)))
        {
            var dayFrequencies = jobsByDate.TryGetValue(date, out var dayJobs)
                ? AnalyzePostings(dayJobs, definitions)
                : [];
            var frequencyBySlug = dayFrequencies.ToDictionary(
                x => x.SkillSlug,
                x => x,
                StringComparer.OrdinalIgnoreCase);

            foreach (var skillSlug in visibleSkillSlugs)
            {
                frequencyBySlug.TryGetValue(skillSlug, out var frequency);
                definitionBySlug.TryGetValue(skillSlug, out var definition);

                points.Add(new MarketTrendPointDto
                {
                    Date = date.ToDateTime(TimeOnly.MinValue),
                    SkillName = frequency?.SkillName ?? definition?.Name ?? skillSlug,
                    SkillSlug = skillSlug,
                    MentionCount = frequency?.MentionCount ?? 0,
                    PostingCount = frequency?.PostingCount ?? 0
                });
            }
        }

        return points;
    }

    private static IReadOnlyList<JobMarketPosting> Deduplicate(IEnumerable<JobMarketPosting> jobs)
    {
        return jobs
            .Where(HasStableIdentity)
            .GroupBy(BuildIdentityKey, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static bool HasStableIdentity(JobMarketPosting job)
    {
        return !string.IsNullOrWhiteSpace(job.Id) ||
            !string.IsNullOrWhiteSpace(job.Url) ||
            !string.IsNullOrWhiteSpace(job.Title);
    }

    private static string BuildIdentityKey(JobMarketPosting job)
    {
        if (!string.IsNullOrWhiteSpace(job.Id))
        {
            return $"id:{job.Id.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(job.Url))
        {
            return $"url:{job.Url.Trim()}";
        }

        return string.Join(
            ':',
            "job",
            job.Title?.Trim() ?? string.Empty,
            job.Company?.Trim() ?? string.Empty,
            job.Location?.Trim() ?? string.Empty,
            job.PostedOn?.ToString("O") ?? string.Empty);
    }

    private static IReadOnlyDictionary<string, int> ToMentionLookup(
        IEnumerable<JobMarketKeywordFrequency> frequencies)
    {
        return frequencies.ToDictionary(
            x => x.SkillSlug,
            x => x.MentionCount,
            StringComparer.OrdinalIgnoreCase);
    }

    private static MarketSkillSummaryDto ToSkillSummary(
        JobMarketKeywordFrequency frequency,
        int currentMentionCount,
        int previousMentionCount)
    {
        return new MarketSkillSummaryDto
        {
            SkillName = frequency.SkillName,
            SkillSlug = frequency.SkillSlug,
            MentionCount = frequency.MentionCount,
            PostingCount = frequency.PostingCount,
            GrowthPercent = CalculateGrowth(currentMentionCount, previousMentionCount)
        };
    }

    private static IReadOnlyList<string> ResolveVisibleSkillSlugs(
        IReadOnlyCollection<string> selectedSkillSlugs,
        IReadOnlyCollection<MarketSkillSummaryDto> skills,
        int maxVisibleSkills)
    {
        var selected = selectedSkillSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxVisibleSkills)
            .ToList();

        return selected.Count > 0
            ? selected
            : skills.Take(maxVisibleSkills).Select(x => x.SkillSlug).ToList();
    }

    private static IReadOnlyList<MarketSegmentSummaryDto> BuildSegmentSummaries(
        IEnumerable<string?> values,
        int total,
        int take)
    {
        return values
            .Select(NormalizeSegmentName)
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => new MarketSegmentSummaryDto
            {
                Name = x.Key,
                Count = x.Count(),
                Percent = total <= 0 ? 0 : Math.Round((decimal)x.Count() / total * 100, 1)
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(Math.Max(1, take))
            .ToList();
    }

    private static string BuildSearchDocument(JobMarketPosting job)
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

        return NormalizeWhitespace(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static MarketJobPostingDto ToJobPostingDto(JobMarketPosting job)
    {
        return new MarketJobPostingDto
        {
            Id = FirstNonEmpty(job.Id, job.Url, job.Title) ?? "job",
            Title = TrimTo(job.Title, 250) ?? "Untitled IT job",
            Company = TrimTo(job.Company, 160),
            Category = TrimTo(job.Category, 100),
            Location = TrimTo(job.Location, 160),
            Salary = TrimTo(job.Salary, 100),
            Experience = TrimTo(job.Experience, 100),
            PostDate = job.PostedOn?.ToDateTime(TimeOnly.MinValue),
            PostDateText = TrimTo(job.PostedOnText, 80),
            Url = TrimTo(job.Url, 500) ?? string.Empty,
            IsActive = job.IsActive,
            Requirements = CleanList(job.Requirements),
            Specialties = CleanList(job.Specialties)
        };
    }

    private static int CountSources(IReadOnlyCollection<JobMarketPosting> jobs)
    {
        var sourceCount = jobs
            .Select(x =>
            {
                if (!Uri.TryCreate(x.Url, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    ? uri.Host[4..]
                    : uri.Host;
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return sourceCount > 0 || jobs.Count == 0 ? sourceCount : 1;
    }

    private static DateOnly? GetLatestPostDate(IEnumerable<JobMarketPosting> jobs)
    {
        var dates = jobs
            .Select(x => x.PostedOn)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return dates.Count == 0 ? null : dates.Max();
    }

    private static DateTime? GetLatestUpdatedAt(IEnumerable<JobMarketPosting> jobs)
    {
        var values = jobs
            .Select(x => x.UpdatedAt)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return values.Count == 0 ? null : values.Max();
    }

    private static DateTime GetPostDateSortValue(JobMarketPosting job)
    {
        return job.PostedOn?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
    }

    private static DateTime GetUpdatedSortValue(JobMarketPosting job)
    {
        return job.UpdatedAt ?? DateTime.MinValue;
    }

    private static decimal CalculateGrowth(int current, int previous)
    {
        if (previous <= 0)
        {
            return current > 0 ? 100 : 0;
        }

        return Math.Round(((decimal)(current - previous) / previous) * 100, 1);
    }

    private static string NormalizeSegmentName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unspecified"
            : value.Trim();
    }

    private static IReadOnlyList<string> CleanList(IEnumerable<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string NormalizeWhitespace(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
