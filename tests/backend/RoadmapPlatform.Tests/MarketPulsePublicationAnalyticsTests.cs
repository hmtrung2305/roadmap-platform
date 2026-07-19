using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests;

public sealed class MarketPulsePublicationAnalyticsTests
{
    [Fact]
    public void RelativeIntervalContributesExactlyOneAcrossBoundary()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "relative",
                    new DateOnly(2026, 7, 7),
                    new DateOnly(2026, 7, 5),
                    new DateOnly(2026, 7, 11),
                    ["react"])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 7, 14),
            7,
            ["react"]);

        Assert.Equal(4m / 7m, result.CurrentPeriod.RelativeEstimate, 2);
        Assert.Equal(3m / 7m, result.PreviousPeriod.RelativeEstimate, 2);
        Assert.Equal(
            1m,
            result.CurrentPeriod.RelativeEstimate + result.PreviousPeriod.RelativeEstimate,
            2);
    }

    [Fact]
    public void CoveredDayWithoutPostingsIsZeroAndUncoveredDayIsUnavailable()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Null(result.MarketTrendPoints.Single(point => point.Date.Day == 9).TotalEstimate);
        Assert.Equal(0m, result.MarketTrendPoints.Single(point => point.Date.Day == 10).TotalEstimate);
        Assert.False(result.MarketTrendPoints.Single(point => point.Date.Day == 9).Available);
        Assert.True(result.MarketTrendPoints.Single(point => point.Date.Day == 10).Available);
    }

    [Fact]
    public void PreviousZeroIsNewOnlyWhenBothPeriodsAreCovered()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "exact",
                    new DateOnly(2026, 7, 14),
                    new DateOnly(2026, 7, 14),
                    new DateOnly(2026, 7, 14),
                    [])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Equal("new", result.MarketComparison.Direction);
        Assert.Null(result.MarketComparison.GrowthPercent);
    }

    [Fact]
    public void IncompletePreviousCoverageReturnsInsufficient()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "exact",
                    new DateOnly(2026, 7, 14),
                    new DateOnly(2026, 7, 14),
                    new DateOnly(2026, 7, 14),
                    [])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Equal("insufficient", result.MarketComparison.Direction);
    }

    [Fact]
    public void ExactAndRelativeQualityRemainDistinct()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact("exact", new DateOnly(2026, 7, 14), null, null, []),
                new PublicationPostingFact(
                    "relative",
                    new DateOnly(2026, 7, 10),
                    new DateOnly(2026, 7, 8),
                    new DateOnly(2026, 7, 14),
                    []),
                new PublicationPostingFact("unknown", null, null, null, [])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Equal(1, result.PostDateQuality.ExactCount);
        Assert.Equal(1, result.PostDateQuality.RelativeCount);
        Assert.Equal(1, result.PostDateQuality.UnknownCount);
        Assert.Equal(4m, result.PostDateQuality.AverageIntervalWidthDays);
    }

    [Fact]
    public void ExactPostingWithCorruptBroadBoundsStillContributesOne()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "exact",
                    new DateOnly(2026, 7, 12),
                    new DateOnly(2026, 7, 1),
                    new DateOnly(2026, 7, 14),
                    ["dotnet"])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Equal(1m, result.CurrentPeriod.EstimatedTotal);
        Assert.Equal(1, result.CurrentPeriod.ExactCount);
        Assert.Equal(1m, result.SkillComparisons.Single().CurrentTotal);
    }

    [Fact]
    public void EmptySkillSelectionBuildsTopThreeSeriesFromFacts()
    {
        var facts = new[]
        {
            new PublicationPostingFact("exact", new DateOnly(2026, 7, 14), null, null, ["react", "sql"]),
            new PublicationPostingFact("exact", new DateOnly(2026, 7, 13), null, null, ["react", "docker"]),
            new PublicationPostingFact("exact", new DateOnly(2026, 7, 12), null, null, ["react", "python"]),
            new PublicationPostingFact("exact", new DateOnly(2026, 7, 11), null, null, ["sql", "docker"])
        };
        var result = PublicationAnalyticsBuilder.Build(
            facts,
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Equal(3, result.SkillComparisons.Count);
        Assert.Equal("react", result.SkillComparisons[0].SkillSlug);
        Assert.DoesNotContain(result.SkillComparisons, item => item.SkillSlug == "python");
    }

    [Fact]
    public void DefaultSkillsUseWeightedReliableCurrentDemand()
    {
        var facts = Enumerable.Range(1, 20)
            .Select(_ => new PublicationPostingFact("unknown", null, null, null, ["python"]))
            .Append(new PublicationPostingFact(
                "exact",
                new DateOnly(2026, 7, 14),
                null,
                null,
                ["react"]))
            .ToList();

        var result = PublicationAnalyticsBuilder.Build(
            facts,
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 14),
            7,
            []);

        Assert.Single(result.SkillComparisons);
        Assert.Equal("react", result.SkillComparisons.Single().SkillSlug);
    }

    [Fact]
    public void PostingEvidenceExtendsCoverageBeyondAnOlderWatermark()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "relative",
                    new DateOnly(2026, 7, 11),
                    new DateOnly(2026, 7, 8),
                    new DateOnly(2026, 7, 14),
                    ["react"])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 14),
            7,
            ["react"]);

        var visibleTotal = result.SkillTrendPoints
            .Where(point => point.Available)
            .Sum(point => point.TotalEstimate ?? 0);
        var currentTotal = result.SkillComparisons.Single().CurrentTotal;
        Assert.NotNull(currentTotal);
        Assert.Equal(1m, currentTotal.Value);
        // Daily chart points are rounded independently, so their displayed sum
        // can differ slightly from the unrounded period total.
        Assert.InRange(Math.Abs(visibleTotal - currentTotal.Value), 0m, 0.03m);
        Assert.Equal(new DateTime(2026, 7, 8), result.HistoryCoverageStart);
    }

    [Fact]
    public void ReliableRelativeHistoryRendersWithoutCrawlerObservationWatermark()
    {
        var result = PublicationAnalyticsBuilder.Build(
            [
                new PublicationPostingFact(
                    "relative",
                    new DateOnly(2026, 7, 7),
                    new DateOnly(2026, 7, 1),
                    new DateOnly(2026, 7, 14),
                    ["react"])
            ],
            new DateOnly(2026, 7, 14),
            DateTime.UtcNow,
            null,
            null,
            7,
            ["react"]);

        Assert.Equal("available", result.Availability);
        Assert.Equal(new DateTime(2026, 7, 1), result.HistoryCoverageStart);
        Assert.Equal(new DateTime(2026, 7, 14), result.HistoryCoverageEnd);
        Assert.All(result.MarketTrendPoints, point => Assert.True(point.Available));
        Assert.Equal(0.5m, result.CurrentPeriod.EstimatedTotal, 2);
        Assert.Equal(0.5m, result.PreviousPeriod.EstimatedTotal, 2);
        Assert.Equal(
            1m,
            result.CurrentPeriod.EstimatedTotal + result.PreviousPeriod.EstimatedTotal,
            2);
    }
}
