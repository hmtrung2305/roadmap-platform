using RoadmapPlatform.Application.DTOs.MarketPulse;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed record PublicationPostingFact(
    string Confidence,
    DateOnly? RepresentativeDate,
    DateOnly? LowerBound,
    DateOnly? UpperBound,
    IReadOnlyCollection<string> SkillSlugs);

/// <summary>
/// Distributes one posting over its supported publication interval. Every posting contributes
/// a total weight of exactly one, including when its interval crosses period boundaries.
/// </summary>
public static class PublicationAnalyticsBuilder
{
    public static MarketPulsePublicationAnalyticsDto Build(
        IReadOnlyCollection<PublicationPostingFact> facts,
        DateOnly anchorDate,
        DateTime? sourceDataAt,
        DateOnly? coverageStart,
        DateOnly? coverageEnd,
        int days,
        IReadOnlyCollection<string> selectedSkillSlugs)
    {
        days = days is 7 or 14 or 30 or 90 ? days : 30;
        var currentStart = anchorDate.AddDays(-(days - 1));
        var previousEnd = currentStart.AddDays(-1);
        var previousStart = currentStart.AddDays(-days);
        var allStart = previousStart;
        var allEnd = anchorDate;
        var dates = EnumerateDates(allStart, allEnd).ToList();
        var reliableRanges = facts
            .Select(fact => ResolveReliableRange(fact, anchorDate))
            .Where(range => range.HasValue)
            .Select(range => range!.Value)
            .ToList();
        var evidenceStart = reliableRanges.Count > 0
            ? reliableRanges.Min(range => range.Lower)
            : (DateOnly?)null;
        var effectiveCoverageStart = MinDate(coverageStart, evidenceStart);
        var effectiveCoverageEnd = coverageEnd;
        if (reliableRanges.Count > 0 &&
            (!effectiveCoverageEnd.HasValue || effectiveCoverageEnd.Value < anchorDate))
        {
            effectiveCoverageEnd = anchorDate;
        }
        if (effectiveCoverageEnd.HasValue && effectiveCoverageEnd.Value > anchorDate)
        {
            effectiveCoverageEnd = anchorDate;
        }
        var coverageIsKnown = effectiveCoverageStart.HasValue && effectiveCoverageEnd.HasValue;
        var coverageIsInferredFromPostings = reliableRanges.Count > 0 &&
            (!coverageStart.HasValue || evidenceStart!.Value < coverageStart.Value ||
             !coverageEnd.HasValue || coverageEnd.Value < anchorDate);
        bool Available(DateOnly date) =>
            coverageIsKnown && date >= effectiveCoverageStart!.Value &&
            date <= effectiveCoverageEnd!.Value;

        var exact = dates.ToDictionary(date => date, _ => 0m);
        var relative = dates.ToDictionary(date => date, _ => 0m);
        var skillSlugs = selectedSkillSlugs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        if (skillSlugs.Count == 0)
        {
            skillSlugs = ResolveDefaultSkillSlugs(
                facts,
                currentStart,
                anchorDate,
                Available);
        }
        var skillExact = skillSlugs.ToDictionary(
            slug => slug,
            _ => dates.ToDictionary(date => date, _ => 0m),
            StringComparer.OrdinalIgnoreCase);
        var skillRelative = skillSlugs.ToDictionary(
            slug => slug,
            _ => dates.ToDictionary(date => date, _ => 0m),
            StringComparer.OrdinalIgnoreCase);

        var exactCount = 0;
        var relativeCount = 0;
        var unknownCount = 0;
        var intervalWidths = new List<int>();
        var broadIntervals = 0;

        foreach (var fact in facts)
        {
            var dateConfidence = MarketPulseBusinessTime.NormalizePostDateConfidence(fact.Confidence);
            if (dateConfidence == "unknown")
            {
                unknownCount++;
                continue;
            }

            var lower = fact.LowerBound ?? fact.RepresentativeDate;
            var upper = fact.UpperBound ?? fact.RepresentativeDate;
            if (!lower.HasValue || !upper.HasValue)
            {
                unknownCount++;
                continue;
            }
            if (upper < lower)
            {
                (lower, upper) = (upper, lower);
            }

            var isExact = dateConfidence == "exact";
            if (isExact)
            {
                lower = fact.RepresentativeDate ?? lower;
                upper = lower;
            }
            var width = upper.Value.DayNumber - lower.Value.DayNumber + 1;
            intervalWidths.Add(width);
            if (width > 7)
            {
                broadIntervals++;
            }

            if (isExact)
            {
                exactCount++;
            }
            else
            {
                relativeCount++;
            }

            var perDay = 1m / width;
            var factSkills = fact.SkillSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var date in EnumerateDates(
                         lower.Value < allStart ? allStart : lower.Value,
                         upper.Value > allEnd ? allEnd : upper.Value))
            {
                if (date < lower || date > upper)
                {
                    continue;
                }

                var amount = isExact ? 1m : perDay;
                if (isExact)
                {
                    exact[date] += amount;
                }
                else
                {
                    relative[date] += amount;
                }

                foreach (var slug in skillSlugs.Where(factSkills.Contains))
                {
                    (isExact ? skillExact[slug] : skillRelative[slug])[date] += amount;
                }
            }
        }

        var points = dates
            .Where(date => date >= currentStart)
            .Select(date => new MarketPulsePublicationTrendPointDto
            {
                Date = date.ToDateTime(TimeOnly.MinValue),
                Available = Available(date),
                ExactPostings = Available(date) ? Round(exact[date]) : null,
                RelativeEstimate = Available(date) ? Round(relative[date]) : null,
                TotalEstimate = Available(date) ? Round(exact[date] + relative[date]) : null
            })
            .ToList();

        var current = BuildPeriod(currentStart, anchorDate, days, exact, relative, Available);
        var previous = BuildPeriod(previousStart, previousEnd, days, exact, relative, Available);
        var sampleSize = exactCount + relativeCount + unknownCount;
        var reliablePercent = Percent(exactCount + relativeCount, sampleSize);
        var broadShare = Percent(broadIntervals, exactCount + relativeCount);
        var dataAgeHours = sourceDataAt.HasValue
            ? Math.Max(0, (DateTime.UtcNow - NormalizeUtc(sourceDataAt.Value)).TotalHours)
            : double.MaxValue;
        var confidence = ResolveConfidence(sampleSize, reliablePercent, broadShare, dataAgeHours);
        if (coverageIsInferredFromPostings && confidence == "high")
        {
            confidence = "medium";
        }
        var completePrevious = previous.CoveredDays == days;
        var completeCurrent = current.CoveredDays == days;

        var analytics = new MarketPulsePublicationAnalyticsDto
        {
            AnchorDate = anchorDate.ToDateTime(TimeOnly.MinValue),
            SourceDataAt = sourceDataAt,
            HistoryCoverageStart = effectiveCoverageStart?.ToDateTime(TimeOnly.MinValue),
            HistoryCoverageEnd = effectiveCoverageEnd?.ToDateTime(TimeOnly.MinValue),
            Availability = !coverageIsKnown
                ? reliableRanges.Count == 0 ? "no_reliable_dates" : "insufficient_history"
                : completeCurrent && completePrevious
                    ? "available"
                    : "insufficient_history",
            Confidence = confidence,
            CurrentPeriod = current,
            PreviousPeriod = previous,
            MarketTrendPoints = points,
            MarketComparison = Compare(
                current.EstimatedTotal,
                previous.EstimatedTotal,
                current.AveragePerDay,
                previous.AveragePerDay,
                completeCurrent,
                completePrevious,
                confidence),
            PostDateQuality = new MarketPulsePostDateQualityDto
            {
                SampleSize = sampleSize,
                ExactCount = exactCount,
                RelativeCount = relativeCount,
                UnknownCount = unknownCount,
                ExactPercent = Percent(exactCount, sampleSize),
                RelativePercent = Percent(relativeCount, sampleSize),
                UnknownPercent = Percent(unknownCount, sampleSize),
                ReliablePercent = reliablePercent,
                AverageIntervalWidthDays = intervalWidths.Count == 0
                    ? 0
                    : Round(intervalWidths.Average()),
                BroadRangeSharePercent = broadShare,
                Confidence = confidence
            }
        };

        analytics.SkillTrendPoints = skillSlugs
            .SelectMany(slug => dates
                .Where(date => date >= currentStart)
                .Select(date => new MarketPulsePublicationSkillTrendPointDto
                {
                    Date = date.ToDateTime(TimeOnly.MinValue),
                    SkillSlug = slug,
                    SkillName = Humanize(slug),
                    Available = Available(date),
                    ExactPostings = Available(date) ? Round(skillExact[slug][date]) : null,
                    RelativeEstimate = Available(date) ? Round(skillRelative[slug][date]) : null,
                    TotalEstimate = Available(date)
                        ? Round(skillExact[slug][date] + skillRelative[slug][date])
                        : null
                }))
            .ToList();
        analytics.SkillComparisons = skillSlugs.Select(slug =>
        {
            var currentTotal = Sum(
                skillExact[slug],
                skillRelative[slug],
                currentStart,
                anchorDate,
                Available);
            var previousTotal = Sum(
                skillExact[slug],
                skillRelative[slug],
                previousStart,
                previousEnd,
                Available);
            var comparison = Compare(
                currentTotal,
                previousTotal,
                current.CoveredDays > 0 ? currentTotal / current.CoveredDays : null,
                previous.CoveredDays > 0 ? previousTotal / previous.CoveredDays : null,
                completeCurrent,
                completePrevious,
                confidence);
            return new MarketPulsePublicationSkillComparisonDto
            {
                SkillSlug = slug,
                SkillName = Humanize(slug),
                CurrentTotal = Round(currentTotal),
                PreviousTotal = Round(previousTotal),
                CurrentAverage = comparison.CurrentAverage,
                PreviousAverage = comparison.PreviousAverage,
                Delta = comparison.Delta,
                GrowthPercent = comparison.GrowthPercent,
                Direction = comparison.Direction,
                Confidence = confidence
            };
        }).ToList();
        return analytics;
    }

    private static MarketPulsePublicationPeriodDto BuildPeriod(
        DateOnly start,
        DateOnly end,
        int expectedDays,
        IReadOnlyDictionary<DateOnly, decimal> exact,
        IReadOnlyDictionary<DateOnly, decimal> relative,
        Func<DateOnly, bool> available)
    {
        var dates = EnumerateDates(start, end).ToList();
        var coveredDates = dates.Where(available).ToList();
        var exactTotal = coveredDates.Sum(date => exact.GetValueOrDefault(date));
        var relativeTotal = coveredDates.Sum(date => relative.GetValueOrDefault(date));
        var total = exactTotal + relativeTotal;
        return new MarketPulsePublicationPeriodDto
        {
            StartDate = start.ToDateTime(TimeOnly.MinValue),
            EndDate = end.ToDateTime(TimeOnly.MinValue),
            ExpectedDays = expectedDays,
            CoveredDays = coveredDates.Count,
            EstimatedTotal = Round(total),
            ExactCount = (int)exactTotal,
            RelativeEstimate = Round(relativeTotal),
            AveragePerDay = coveredDates.Count == 0 ? null : Round(total / coveredDates.Count)
        };
    }

    private static MarketPulsePublicationComparisonDto Compare(
        decimal currentTotal,
        decimal previousTotal,
        decimal? currentAverage,
        decimal? previousAverage,
        bool currentComplete,
        bool previousComplete,
        string confidence)
    {
        var comparison = new MarketPulsePublicationComparisonDto
        {
            CurrentTotal = Round(currentTotal),
            PreviousTotal = Round(previousTotal),
            CurrentAverage = currentAverage.HasValue ? Round(currentAverage.Value) : null,
            PreviousAverage = previousAverage.HasValue ? Round(previousAverage.Value) : null,
            Delta = Round(currentTotal - previousTotal),
            Confidence = confidence
        };
        if (!currentComplete || !previousComplete)
        {
            return comparison;
        }
        if (previousTotal == 0)
        {
            comparison.Direction = currentTotal > 0 ? "new" : "flat";
            return comparison;
        }

        comparison.GrowthPercent = Round((currentTotal - previousTotal) / previousTotal * 100);
        comparison.Direction = currentTotal > previousTotal
            ? "up"
            : currentTotal < previousTotal
                ? "down"
                : "flat";
        return comparison;
    }

    private static decimal Sum(
        IReadOnlyDictionary<DateOnly, decimal> exact,
        IReadOnlyDictionary<DateOnly, decimal> relative,
        DateOnly start,
        DateOnly end,
        Func<DateOnly, bool> available) =>
        EnumerateDates(start, end).Where(available).Sum(date =>
            exact.GetValueOrDefault(date) + relative.GetValueOrDefault(date));

    private static List<string> ResolveDefaultSkillSlugs(
        IReadOnlyCollection<PublicationPostingFact> facts,
        DateOnly currentStart,
        DateOnly currentEnd,
        Func<DateOnly, bool> available)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var fact in facts)
        {
            var confidence = MarketPulseBusinessTime.NormalizePostDateConfidence(fact.Confidence);
            if (confidence == "unknown")
            {
                continue;
            }

            var lower = fact.LowerBound ?? fact.RepresentativeDate;
            var upper = fact.UpperBound ?? fact.RepresentativeDate;
            if (!lower.HasValue || !upper.HasValue)
            {
                continue;
            }
            if (upper < lower)
            {
                (lower, upper) = (upper, lower);
            }
            if (confidence == "exact")
            {
                lower = fact.RepresentativeDate ?? lower;
                upper = lower;
            }

            var intervalWidth = upper.Value.DayNumber - lower.Value.DayNumber + 1;
            var overlapStart = lower.Value > currentStart ? lower.Value : currentStart;
            var overlapEnd = upper.Value < currentEnd ? upper.Value : currentEnd;
            if (overlapStart > overlapEnd)
            {
                continue;
            }

            var coveredDays = EnumerateDates(overlapStart, overlapEnd).Count(available);
            var contribution = (decimal)coveredDays / intervalWidth;
            if (contribution <= 0)
            {
                continue;
            }

            foreach (var slug in fact.SkillSlugs
                         .Where(value => !string.IsNullOrWhiteSpace(value))
                         .Select(value => value.Trim().ToLowerInvariant())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                totals[slug] = totals.GetValueOrDefault(slug) + contribution;
            }
        }

        return totals
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(item => item.Key)
            .ToList();
    }

    private static string ResolveConfidence(
        int sample,
        decimal reliablePercent,
        decimal broadShare,
        double ageHours)
    {
        if (sample >= 100 && reliablePercent >= 80 && broadShare <= 20 && ageHours <= 24)
        {
            return "high";
        }
        return sample >= 30 && reliablePercent >= 50 && broadShare <= 50 && ageHours <= 48
            ? "medium"
            : "low";
    }

    private static (DateOnly Lower, DateOnly Upper)? ResolveReliableRange(
        PublicationPostingFact fact,
        DateOnly anchorDate)
    {
        var confidence = MarketPulseBusinessTime.NormalizePostDateConfidence(fact.Confidence);
        if (confidence == "unknown")
        {
            return null;
        }

        var lower = fact.LowerBound ?? fact.RepresentativeDate;
        var upper = fact.UpperBound ?? fact.RepresentativeDate;
        if (!lower.HasValue || !upper.HasValue)
        {
            return null;
        }
        if (upper < lower)
        {
            (lower, upper) = (upper, lower);
        }
        if (confidence == "exact")
        {
            lower = fact.RepresentativeDate ?? lower;
            upper = lower;
        }
        if (lower > anchorDate)
        {
            return null;
        }

        return (lower.Value, upper.Value > anchorDate ? anchorDate : upper.Value);
    }

    private static DateOnly? MinDate(DateOnly? left, DateOnly? right)
    {
        if (!left.HasValue)
        {
            return right;
        }
        if (!right.HasValue)
        {
            return left;
        }
        return left.Value <= right.Value ? left : right;
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static string Humanize(string slug) => string.Join(
        ' ',
        slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => char.ToUpperInvariant(value[0]) + value[1..]));

    private static decimal Percent(int numerator, int denominator) =>
        denominator <= 0 ? 0 : Round((decimal)numerator / denominator * 100);

    private static decimal Round(decimal value) => Math.Round(value, 2);

    private static decimal Round(double value) => Math.Round((decimal)value, 2);

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
