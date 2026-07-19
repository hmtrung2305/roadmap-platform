using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Models.MarketPulse;

namespace RoadmapPlatform.Application.Services.MarketPulse;

public sealed class JobMarketOverviewBuilder(JobMarketKeywordAnalyzer keywordAnalyzer)
{
    private const string MarketSourceName = "TopCV";

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
        var lastUpdatedAt = GetLatestUpdatedAt(activeJobs.Concat(todayJobs));
        var sourceSummaries = BuildSegmentSummaries(
            activeJobs.Select(_ => MarketSourceName),
            activeJobs.Count,
            options.MaxSegmentCount);
        var sourceCount = sourceSummaries.Count;
        var dataQuality = BuildDataQuality(activeJobs, lastUpdatedAt, sourceCount);
        var insightMeta = BuildInsightMeta(days, activeJobs.Count, dataQuality, lastUpdatedAt);
        var skillMovements = BuildSkillMovements(activeJobs, definitions, days, reportDate);
        var coOccurrences = BuildSkillCoOccurrences(activeJobs, definitions);
        var salaryInsight = BuildSalaryInsight(activeJobs);
        var experienceSummaries = BuildSegmentSummaries(
            activeJobs.Select(ResolveExperienceLevel),
            activeJobs.Count,
            options.MaxSegmentCount);
        var insightCards = BuildInsightCards(
            activeJobs,
            todayJobs,
            skillMovements.Rising,
            salaryInsight,
            dataQuality,
            days,
            lastUpdatedAt);

        return new MarketPulseOverviewDto
        {
            LastUpdatedAt = lastUpdatedAt,
            TotalPostings = Math.Max(snapshot.ActiveTotal, activeJobs.Count),
            ActivePostings = Math.Max(snapshot.ActiveTotal, activeJobs.Count),
            TodayPostings = Math.Max(snapshot.TodayTotal, todayJobs.Count),
            StalePostings = 0,
            ExpiredPostings = snapshot.ActiveJobs.Count(x => !x.IsActive),
            SourceCount = sourceCount,
            Skills = skills,
            TodaySkills = todaySkills,
            TrendPoints = BuildTrendPoints(activeJobs, definitions, visibleSkillSlugs, days, reportDate),
            CategorySummaries = BuildSegmentSummaries(activeJobs.Select(x => x.Category), activeJobs.Count, options.MaxSegmentCount),
            LocationSummaries = BuildSegmentSummaries(activeJobs.Select(x => x.Location), activeJobs.Count, options.MaxSegmentCount),
            SourceSummaries = sourceSummaries,
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
                .ToList(),
            InsightMeta = insightMeta,
            DataQuality = dataQuality,
            InsightCards = insightCards,
            RisingSkills = skillMovements.Rising,
            FallingSkills = skillMovements.Falling,
            SkillCoOccurrences = coOccurrences,
            SalaryInsight = salaryInsight,
            ExperienceSummaries = experienceSummaries,
            LearningRecommendations = BuildLearningRecommendations(
                skillMovements.Rising,
                coOccurrences,
                dataQuality)
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

    public IReadOnlyList<string> ResolveSkillSlugs(
        JobMarketPosting posting,
        IReadOnlyCollection<string> trackedKeywordSpecs)
    {
        var definitions = keywordAnalyzer.BuildDefinitions(trackedKeywordSpecs);
        return AnalyzePostings([posting], definitions)
            .Select(x => x.SkillSlug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private MarketDataQualityDto BuildDataQuality(
        IReadOnlyCollection<JobMarketPosting> jobs,
        DateTime? lastUpdatedAt,
        int sourceCount)
    {
        var total = jobs.Count;
        var salaryCoverage = Percent(jobs.Count(HasSalarySignal), total);
        var categoryMissing = jobs.Count(x => IsMissingCategory(x.Category));
        var categoryCoverage = Percent(total - categoryMissing, total);
        var locationCoverage = Percent(jobs.Count(x => !IsMissingSegment(x.Location)), total);
        var detailCoverage = Percent(jobs.Count(HasDetailSignal), total);
        var otherCategoryPercent = Percent(categoryMissing, total);
        var freshnessHours = lastUpdatedAt.HasValue
            ? Math.Max(0, (int)Math.Round((DateTime.UtcNow - NormalizeUtc(lastUpdatedAt.Value)).TotalHours))
            : 999;
        var freshnessScore = freshnessHours <= 6 ? 100m : freshnessHours <= 24 ? 70m : 30m;
        var score = Math.Round(
            salaryCoverage * 0.20m +
            categoryCoverage * 0.25m +
            locationCoverage * 0.20m +
            detailCoverage * 0.25m +
            freshnessScore * 0.10m,
            1);
        var warnings = new List<string>();

        if (total == 0)
        {
            warnings.Add("No active jobs are available for this snapshot.");
        }
        if (freshnessHours > 24)
        {
            warnings.Add("Latest market snapshot is older than 24 hours.");
        }
        if (detailCoverage < 90)
        {
            warnings.Add("Detail enrichment coverage is below 90 percent.");
        }
        if (otherCategoryPercent > 15)
        {
            warnings.Add("Category Other/missing rate is above 15 percent.");
        }
        if (salaryCoverage < 50)
        {
            warnings.Add("Salary coverage is below 50 percent; salary analysis has limited confidence.");
        }
        if (locationCoverage < 70)
        {
            warnings.Add("Location coverage is below 70 percent.");
        }
        return new MarketDataQualityDto
        {
            Score = score,
            Level = ConfidenceForQuality(score, total),
            SampleSize = total,
            SourceCount = sourceCount,
            SalaryCoveragePercent = salaryCoverage,
            CategoryCoveragePercent = categoryCoverage,
            LocationCoveragePercent = locationCoverage,
            DetailCoveragePercent = detailCoverage,
            OtherCategoryPercent = otherCategoryPercent,
            FreshnessHours = freshnessHours,
            Warnings = warnings
        };
    }

    private static MarketInsightMetaDto BuildInsightMeta(
        int days,
        int sampleSize,
        MarketDataQualityDto dataQuality,
        DateTime? lastUpdatedAt)
    {
        return new MarketInsightMetaDto
        {
            PeriodDays = days,
            SampleSize = sampleSize,
            Confidence = dataQuality.Level,
            LastUpdatedAt = lastUpdatedAt,
            Methodology = "Demand trends use publication dates. Exact dates count on one day; relative week/month ranges contribute one posting distributed evenly across their supported interval. Dates outside verified history coverage remain unavailable."
        };
    }

    private (IReadOnlyList<MarketSkillMovementDto> Rising, IReadOnlyList<MarketSkillMovementDto> Falling) BuildSkillMovements(
        IReadOnlyCollection<JobMarketPosting> jobs,
        IReadOnlyCollection<JobMarketKeywordDefinition> definitions,
        int days,
        DateOnly reportDate)
    {
        var currentStart = reportDate.AddDays(-(days - 1));
        var previousStart = currentStart.AddDays(-days);
        var previousEnd = currentStart.AddDays(-1);
        var currentJobs = jobs
            .Where(x => x.PostedOn.HasValue && x.PostedOn.Value >= currentStart && x.PostedOn.Value <= reportDate)
            .ToList();
        var previousJobs = jobs
            .Where(x => x.PostedOn.HasValue && x.PostedOn.Value >= previousStart && x.PostedOn.Value <= previousEnd)
            .ToList();
        var current = AnalyzePostings(currentJobs, definitions)
            .ToDictionary(x => x.SkillSlug, x => x, StringComparer.OrdinalIgnoreCase);
        var previous = AnalyzePostings(previousJobs, definitions)
            .ToDictionary(x => x.SkillSlug, x => x, StringComparer.OrdinalIgnoreCase);
        var definitionsBySlug = definitions.ToDictionary(x => x.Slug, x => x, StringComparer.OrdinalIgnoreCase);
        var allSlugs = current.Keys
            .Concat(previous.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var sampleSize = currentJobs.Count + previousJobs.Count;
        var confidence = ConfidenceForSample(sampleSize);
        var movements = allSlugs
            .Select(slug =>
            {
                current.TryGetValue(slug, out var currentFrequency);
                previous.TryGetValue(slug, out var previousFrequency);
                definitionsBySlug.TryGetValue(slug, out var definition);
                var currentMentions = currentFrequency?.MentionCount ?? 0;
                var previousMentions = previousFrequency?.MentionCount ?? 0;

                return new MarketSkillMovementDto
                {
                    SkillName = currentFrequency?.SkillName ?? previousFrequency?.SkillName ?? definition?.Name ?? slug,
                    SkillSlug = slug,
                    CurrentMentions = currentMentions,
                    PreviousMentions = previousMentions,
                    Delta = currentMentions - previousMentions,
                    GrowthPercent = CalculateGrowth(currentMentions, previousMentions),
                    SampleSize = sampleSize,
                    PeriodDays = days,
                    Confidence = confidence
                };
            })
            .ToList();

        return (
            movements
                .Where(x => x.Delta > 0)
                .OrderByDescending(x => x.Delta)
                .ThenByDescending(x => x.CurrentMentions)
                .Take(6)
                .ToList(),
            movements
                .Where(x => x.Delta < 0)
                .OrderBy(x => x.Delta)
                .ThenByDescending(x => x.PreviousMentions)
                .Take(6)
                .ToList());
    }

    private IReadOnlyList<MarketSkillCoOccurrenceDto> BuildSkillCoOccurrences(
        IReadOnlyCollection<JobMarketPosting> jobs,
        IReadOnlyCollection<JobMarketKeywordDefinition> definitions)
    {
        var sampleJobs = jobs
            .Where(x => !string.IsNullOrWhiteSpace(BuildSearchDocument(x)))
            .ToList();
        var pairCounts = new Dictionary<string, (JobMarketKeywordDefinition A, JobMarketKeywordDefinition B, int Count)>(
            StringComparer.OrdinalIgnoreCase);
        var definitionsBySlug = definitions.ToDictionary(x => x.Slug, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var job in sampleJobs)
        {
            var matchedSlugs = AnalyzePostings(new[] { job }, definitions)
                .Select(x => x.SkillSlug)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (var i = 0; i < matchedSlugs.Count; i++)
            {
                for (var j = i + 1; j < matchedSlugs.Count; j++)
                {
                    if (!definitionsBySlug.TryGetValue(matchedSlugs[i], out var first) ||
                        !definitionsBySlug.TryGetValue(matchedSlugs[j], out var second))
                    {
                        continue;
                    }

                    var key = $"{first.Slug}|{second.Slug}";
                    pairCounts.TryGetValue(key, out var current);
                    pairCounts[key] = (first, second, current.Count + 1);
                }
            }
        }

        var confidence = ConfidenceForSample(sampleJobs.Count);
        return pairCounts.Values
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.A.Name)
            .Take(8)
            .Select(x => new MarketSkillCoOccurrenceDto
            {
                SkillA = x.A.Name,
                SkillASlug = x.A.Slug,
                SkillB = x.B.Name,
                SkillBSlug = x.B.Slug,
                PostingCount = x.Count,
                PercentOfSample = Percent(x.Count, sampleJobs.Count),
                SampleSize = sampleJobs.Count,
                Confidence = confidence
            })
            .ToList();
    }

    private static MarketSalaryInsightDto BuildSalaryInsight(IReadOnlyCollection<JobMarketPosting> jobs)
    {
        var ranges = jobs
            .Select(x => TryParseSalary(x.Salary))
            .OfType<SalaryRange>()
            .ToList();
        var confidence = ConfidenceForSample(ranges.Count);

        return new MarketSalaryInsightDto
        {
            SampleSize = ranges.Count,
            CoveragePercent = Percent(ranges.Count, jobs.Count),
            MedianMinMonthlyVnd = Median(ranges.Select(x => x.MinMonthlyVnd).OfType<decimal>().ToList()),
            MedianMaxMonthlyVnd = Median(ranges.Select(x => x.MaxMonthlyVnd).OfType<decimal>().ToList()),
            LowestMonthlyVnd = ranges
                .SelectMany(x => new[] { x.MinMonthlyVnd, x.MaxMonthlyVnd })
                .OfType<decimal>()
                .OrderBy(x => x)
                .Cast<decimal?>()
                .FirstOrDefault(),
            HighestMonthlyVnd = ranges
                .SelectMany(x => new[] { x.MinMonthlyVnd, x.MaxMonthlyVnd })
                .OfType<decimal>()
                .OrderByDescending(x => x)
                .Cast<decimal?>()
                .FirstOrDefault(),
            Confidence = confidence,
            ByCategory = jobs
                .GroupBy(x => NormalizeSegmentName(x.Category), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var groupRanges = group
                        .Select(x => TryParseSalary(x.Salary))
                        .OfType<SalaryRange>()
                        .ToList();

                    return new MarketSalarySegmentDto
                    {
                        Name = group.Key,
                        SampleSize = groupRanges.Count,
                        CoveragePercent = Percent(groupRanges.Count, group.Count()),
                        MedianMinMonthlyVnd = Median(groupRanges.Select(x => x.MinMonthlyVnd).OfType<decimal>().ToList()),
                        MedianMaxMonthlyVnd = Median(groupRanges.Select(x => x.MaxMonthlyVnd).OfType<decimal>().ToList())
                    };
                })
                .Where(x => x.SampleSize > 0)
                .OrderByDescending(x => x.SampleSize)
                .ThenBy(x => x.Name)
                .Take(6)
                .ToList()
        };
    }

    private static IReadOnlyList<MarketPulseInsightDto> BuildInsightCards(
        IReadOnlyCollection<JobMarketPosting> activeJobs,
        IReadOnlyCollection<JobMarketPosting> todayJobs,
        IReadOnlyList<MarketSkillMovementDto> risingSkills,
        MarketSalaryInsightDto salaryInsight,
        MarketDataQualityDto dataQuality,
        int days,
        DateTime? lastUpdatedAt)
    {
        var sampleSize = activeJobs.Count;
        var topCategory = activeJobs
            .Select(x => NormalizeSegmentName(x.Category))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .FirstOrDefault();
        var topLocation = activeJobs
            .Select(x => NormalizeSegmentName(x.Location))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .FirstOrDefault();
        var topRising = risingSkills.FirstOrDefault();

        return
        [
            new MarketPulseInsightDto
            {
                Title = "Market size",
                Value = $"{sampleSize:N0} active",
                Detail = $"{todayJobs.Count:N0} jobs were posted in the latest daily slice.",
                Tone = sampleSize >= 100 ? "positive" : "neutral",
                SampleSize = sampleSize,
                PeriodDays = days,
                Confidence = dataQuality.Level,
                LastUpdatedAt = lastUpdatedAt
            },
            new MarketPulseInsightDto
            {
                Title = "Top rising skill",
                Value = topRising is null ? "No movement" : topRising.SkillName,
                Detail = topRising is null
                    ? "Not enough dated postings to compare against the previous period."
                    : $"{topRising.Delta:+#;-#;0} mentions compared with the previous {days}d window.",
                Tone = topRising is null ? "neutral" : "positive",
                SampleSize = topRising?.SampleSize ?? sampleSize,
                PeriodDays = days,
                Confidence = topRising?.Confidence ?? dataQuality.Level,
                LastUpdatedAt = lastUpdatedAt
            },
            new MarketPulseInsightDto
            {
                Title = "Role demand",
                Value = topCategory?.Key ?? "Unspecified",
                Detail = topCategory is null
                    ? "No category distribution available."
                    : $"{Percent(topCategory.Count(), sampleSize):0.#}% of active postings are in this category.",
                Tone = "neutral",
                SampleSize = sampleSize,
                PeriodDays = days,
                Confidence = dataQuality.Level,
                LastUpdatedAt = lastUpdatedAt
            },
            new MarketPulseInsightDto
            {
                Title = "Location bias",
                Value = topLocation?.Key ?? "Unspecified",
                Detail = topLocation is null
                    ? "No location distribution available."
                    : $"{Percent(topLocation.Count(), sampleSize):0.#}% of the sample is concentrated here.",
                Tone = topLocation is not null && Percent(topLocation.Count(), sampleSize) >= 70 ? "warning" : "neutral",
                SampleSize = sampleSize,
                PeriodDays = days,
                Confidence = dataQuality.Level,
                LastUpdatedAt = lastUpdatedAt
            },
            new MarketPulseInsightDto
            {
                Title = "Salary signal",
                Value = $"{salaryInsight.CoveragePercent:0.#}% covered",
                Detail = salaryInsight.MedianMinMonthlyVnd.HasValue || salaryInsight.MedianMaxMonthlyVnd.HasValue
                    ? $"Median range is {FormatMoney(salaryInsight.MedianMinMonthlyVnd)} - {FormatMoney(salaryInsight.MedianMaxMonthlyVnd)} VND/month."
                    : "Salary strings are mostly missing or negotiable.",
                Tone = salaryInsight.CoveragePercent >= 60 ? "positive" : "warning",
                SampleSize = salaryInsight.SampleSize,
                PeriodDays = days,
                Confidence = salaryInsight.Confidence,
                LastUpdatedAt = lastUpdatedAt
            },
            new MarketPulseInsightDto
            {
                Title = "Data confidence",
                Value = dataQuality.Level,
                Detail = $"Quality score {dataQuality.Score:0.#}/100 from freshness and salary, category, location, and detail coverage.",
                Tone = dataQuality.Level == "high" ? "positive" : dataQuality.Level == "medium" ? "neutral" : "warning",
                SampleSize = sampleSize,
                PeriodDays = days,
                Confidence = dataQuality.Level,
                LastUpdatedAt = lastUpdatedAt
            }
        ];
    }

    private static IReadOnlyList<MarketLearningRecommendationDto> BuildLearningRecommendations(
        IReadOnlyList<MarketSkillMovementDto> risingSkills,
        IReadOnlyList<MarketSkillCoOccurrenceDto> coOccurrences,
        MarketDataQualityDto dataQuality)
    {
        var recommendations = risingSkills
            .Take(3)
            .Select(skill => new MarketLearningRecommendationDto
            {
                Title = $"Prioritize {skill.SkillName}",
                Detail = $"{skill.SkillName} is rising in the latest comparison window. Add a focused module, assessment, or portfolio project.",
                ActionLabel = "Map to learning module",
                SkillSlug = skill.SkillSlug,
                Priority = skill.Confidence == "high" ? "high" : "medium",
                SampleSize = skill.SampleSize,
                Confidence = skill.Confidence
            })
            .ToList();

        recommendations.AddRange(coOccurrences
            .Take(2)
            .Select(pair => new MarketLearningRecommendationDto
            {
                Title = $"Bundle {pair.SkillA} + {pair.SkillB}",
                Detail = $"These skills co-occur in {pair.PostingCount:N0} postings, useful for project-based roadmap recommendations.",
                ActionLabel = "Create project suggestion",
                SkillSlug = pair.SkillASlug,
                Priority = pair.Confidence == "high" ? "high" : "medium",
                SampleSize = pair.SampleSize,
                Confidence = pair.Confidence
            }));

        if (dataQuality.Level == "low")
        {
            recommendations.Add(new MarketLearningRecommendationDto
            {
                Title = "Treat recommendations as provisional",
                Detail = "Data quality is currently low, so learning suggestions should be reviewed after the next successful crawl/detail run.",
                ActionLabel = "Review data quality",
                Priority = "high",
                SampleSize = dataQuality.SampleSize,
                Confidence = dataQuality.Level
            });
        }

        return recommendations.Take(6).ToList();
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
            string.Join(' ', job.Skills),
            string.Join(' ', job.Requirements),
            string.Join(' ', job.Specialties),
            string.Join(' ', job.Benefits)
        };

        return NormalizeWhitespace(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static bool HasSalarySignal(JobMarketPosting job)
    {
        return TryParseSalary(job.Salary) is not null;
    }

    private static bool HasDetailSignal(JobMarketPosting job)
    {
        return job.Requirements.Count > 0 ||
            job.Specialties.Count > 0 ||
            job.Benefits.Count > 0 ||
            job.Skills.Count > 0;
    }

    private static bool IsMissingSegment(string? value)
    {
        var normalized = NormalizeSegmentName(value);
        return normalized.Equals("Unspecified", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMissingCategory(string? value)
    {
        var normalized = NormalizeSegmentName(value);
        return IsMissingSegment(normalized) ||
            normalized.Equals("Other", StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveExperienceLevel(JobMarketPosting job)
    {
        var text = RemoveDiacritics($"{job.Title} {job.Experience}".ToLowerInvariant());

        if (string.IsNullOrWhiteSpace(text))
        {
            return "Unspecified";
        }

        if (text.Contains("intern", StringComparison.Ordinal) ||
            text.Contains("thuc tap", StringComparison.Ordinal))
        {
            return "Intern";
        }

        if (text.Contains("fresher", StringComparison.Ordinal) ||
            text.Contains("khong yeu cau", StringComparison.Ordinal) ||
            text.Contains("no experience", StringComparison.Ordinal))
        {
            return "Fresher";
        }

        if (text.Contains("lead", StringComparison.Ordinal) ||
            text.Contains("manager", StringComparison.Ordinal) ||
            text.Contains("architect", StringComparison.Ordinal))
        {
            return "Lead/Manager";
        }

        if (text.Contains("senior", StringComparison.Ordinal))
        {
            return "Senior";
        }

        if (text.Contains("junior", StringComparison.Ordinal))
        {
            return "Junior";
        }

        var years = Regex.Matches(text, @"\d+(?:[.,]\d+)?")
            .Select(match => decimal.TryParse(
                match.Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : (decimal?)null)
            .OfType<decimal>()
            .ToList();

        if (years.Count == 0)
        {
            return "Unspecified";
        }

        var maxYears = years.Max();
        if (maxYears >= 5)
        {
            return "Senior";
        }

        if (maxYears >= 2)
        {
            return "Mid";
        }

        return "Junior";
    }

    private static MarketJobPostingDto ToJobPostingDto(JobMarketPosting job)
    {
        return new MarketJobPostingDto
        {
            Id = FirstNonEmpty(job.Id, job.Url, job.Title) ?? "job",
            Title = TrimTo(job.Title, 250) ?? "Untitled IT job",
            Company = TrimTo(job.Company, 160),
            Source = MarketSourceName,
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

    private static SalaryRange? TryParseSalary(string? salary)
    {
        if (string.IsNullOrWhiteSpace(salary))
        {
            return null;
        }

        var text = RemoveDiacritics(salary.Trim().ToLowerInvariant());
        if (text.Contains("thoa thuan", StringComparison.Ordinal) ||
            text.Contains("thuong luong", StringComparison.Ordinal) ||
            text.Contains("negotiable", StringComparison.Ordinal) ||
            text.Contains("canh tranh", StringComparison.Ordinal))
        {
            return null;
        }

        var values = Regex.Matches(text, @"\d+(?:[.,]\d+)?")
            .Select(match => decimal.TryParse(
                match.Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : (decimal?)null)
            .OfType<decimal>()
            .Where(x => x > 0)
            .ToList();

        if (values.Count == 0)
        {
            return null;
        }

        var multiplier = ResolveSalaryMultiplier(text, values.Max());
        var normalized = values.Select(x => x * multiplier).OrderBy(x => x).ToList();

        if (normalized.Count == 1)
        {
            var value = normalized[0];
            if (text.Contains("toi", StringComparison.Ordinal) ||
                text.Contains("den", StringComparison.Ordinal) ||
                text.Contains("up to", StringComparison.Ordinal))
            {
                return new SalaryRange(null, value);
            }

            if (text.Contains("tu", StringComparison.Ordinal) ||
                text.Contains("tren", StringComparison.Ordinal) ||
                text.Contains("from", StringComparison.Ordinal))
            {
                return new SalaryRange(value, null);
            }

            return new SalaryRange(value, value);
        }

        return new SalaryRange(normalized.First(), normalized.Last());
    }

    private static decimal ResolveSalaryMultiplier(string normalizedText, decimal maxValue)
    {
        if (normalizedText.Contains("usd", StringComparison.Ordinal) ||
            normalizedText.Contains('$', StringComparison.Ordinal))
        {
            return 25_000m;
        }

        if (normalizedText.Contains("trieu", StringComparison.Ordinal) ||
            normalizedText.Contains("tr", StringComparison.Ordinal) ||
            maxValue < 1_000m)
        {
            return 1_000_000m;
        }

        return 1m;
    }

    private static decimal? Median(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var ordered = values.OrderBy(x => x).ToList();
        var middle = ordered.Count / 2;
        if (ordered.Count % 2 == 1)
        {
            return ordered[middle];
        }

        return Math.Round((ordered[middle - 1] + ordered[middle]) / 2, 0);
    }

    private static decimal Percent(int numerator, int denominator)
    {
        return denominator <= 0
            ? 0
            : Math.Round((decimal)numerator / denominator * 100, 1);
    }

    private static string ConfidenceForSample(int sampleSize)
    {
        if (sampleSize >= 100)
        {
            return "high";
        }

        return sampleSize >= 30 ? "medium" : "low";
    }

    private static string ConfidenceForQuality(decimal score, int sampleSize)
    {
        if (score >= 80 && sampleSize >= 100)
        {
            return "high";
        }

        return score >= 60 && sampleSize >= 30 ? "medium" : "low";
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static string FormatMoney(decimal? value)
    {
        if (!value.HasValue)
        {
            return "n/a";
        }

        return value.Value >= 1_000_000m
            ? $"{value.Value / 1_000_000m:0.#}M"
            : value.Value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
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

    private sealed record SalaryRange(decimal? MinMonthlyVnd, decimal? MaxMonthlyVnd);
}
