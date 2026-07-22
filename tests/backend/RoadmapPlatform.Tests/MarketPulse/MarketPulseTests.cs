using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Controllers.MarketPulse;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Tests.MarketPulse;

public sealed class MarketPulseTests
{
    [Fact]
    public async Task TC228_GetOverviewAsync_WithDefaultQuery_ReturnsConsistentThirtyDayTopCvOverview()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:228-1", fixture.Today, skills: ["React", "SQL"]);
        await fixture.AddPostingAsync("topcv:228-2", fixture.Today.AddDays(-5), category: "Frontend", skills: ["React", "TypeScript"]);
        await fixture.AddPostingAsync("topcv:228-3", fixture.Today.AddDays(-12), location: "Ha Noi", skills: ["Java", "Docker"]);
        await fixture.AddPostingAsync("topcv:228-4", fixture.Today.AddDays(-35), experience: "6 years", skills: ["Python", "Azure"]);
        await fixture.AddHistoryCoverageAsync();

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto(),
            CancellationToken.None);

        Assert.Equal(30, overview.InsightMeta.PeriodDays);
        Assert.Equal(30, overview.PublicationAnalytics.CurrentPeriod.ExpectedDays);
        Assert.Equal(30, overview.PublicationAnalytics.PreviousPeriod.ExpectedDays);
        Assert.Equal("published_date", overview.PublicationAnalytics.Basis);
        Assert.Equal(4, overview.TotalPostings);
        Assert.Equal(4, overview.ActivePostings);
        Assert.Equal(4, overview.DataQuality.SampleSize);
        Assert.Equal(4, overview.InsightMeta.SampleSize);
        Assert.Equal(4, overview.CategorySummaries.Sum(item => item.Count));
        Assert.Equal(4, overview.LocationSummaries.Sum(item => item.Count));
        Assert.Equal(4, overview.ExperienceSummaries.Sum(item => item.Count));
        var source = Assert.Single(overview.SourceSummaries);
        Assert.Equal("TopCV", source.Name);
        Assert.Equal(4, source.Count);
        Assert.NotEmpty(overview.Skills);
        Assert.NotEmpty(overview.TrendPoints);
        Assert.NotEmpty(overview.InsightCards);
        Assert.NotNull(overview.DataQuality);
    }

    [Fact]
    public async Task TC229_GetOverviewAsync_WithEachSupportedWindow_RetainsDaysAndBuildsEqualLengthPeriods()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        foreach (var offset in new[] { 0, 6, 13, 29, 30, 59, 89, 120 })
        {
            await fixture.AddPostingAsync(
                $"topcv:229-{offset}",
                fixture.Today.AddDays(-offset),
                skills: [offset % 2 == 0 ? "React" : "SQL"]);
        }
        await fixture.AddHistoryCoverageAsync();

        foreach (var days in new[] { 7, 14, 30, 90 })
        {
            var overview = await fixture.Service.GetOverviewAsync(
                new MarketPulseOverviewQueryDto { Days = days },
                CancellationToken.None);
            var current = overview.PublicationAnalytics.CurrentPeriod;
            var previous = overview.PublicationAnalytics.PreviousPeriod;

            Assert.Equal(days, overview.InsightMeta.PeriodDays);
            Assert.Equal(days, current.ExpectedDays);
            Assert.Equal(days, previous.ExpectedDays);
            Assert.Equal(days, (current.EndDate.Date - current.StartDate.Date).Days + 1);
            Assert.Equal(days, (previous.EndDate.Date - previous.StartDate.Date).Days + 1);
            Assert.Equal(current.StartDate.Date.AddDays(-1), previous.EndDate.Date);
            Assert.Equal("published_date", overview.PublicationAnalytics.Basis);
            Assert.Equal(days, overview.PublicationAnalytics.MarketTrendPoints.Count);
        }
    }

    [Fact]
    public async Task TC230_GetOverviewAsync_WithUnsupportedDays_NormalizesAppliedWindowToThirtyDays()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:230-1", fixture.Today, skills: ["React"]);
        await fixture.AddHistoryCoverageAsync();

        foreach (var unsupportedDays in new[] { 6, 180, 0, -7, 365 })
        {
            var overview = await fixture.Service.GetOverviewAsync(
                new MarketPulseOverviewQueryDto { Days = unsupportedDays },
                CancellationToken.None);

            Assert.Equal(30, overview.InsightMeta.PeriodDays);
            Assert.Equal(30, overview.PublicationAnalytics.CurrentPeriod.ExpectedDays);
            Assert.Equal(30, overview.PublicationAnalytics.PreviousPeriod.ExpectedDays);
            Assert.Equal(30, overview.PublicationAnalytics.MarketTrendPoints.Count);
        }
    }

    [Fact]
    public async Task TC231_GetOverviewAsync_WithMessySkillFilters_NormalizesDeduplicatesAndKeepsFirstSix()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync(
            "topcv:231-1",
            fixture.Today,
            title: "React SQL Docker Python Azure Java Go Engineer",
            skills: ["React", "SQL", "Docker", "Python", "Azure", "Java", "Go"]);
        await fixture.AddHistoryCoverageAsync();
        var rawSkills = new[]
        {
            " React ",
            "REACT",
            "   ",
            " SQL ",
            "Docker",
            " PYTHON ",
            "Azure",
            "Java",
            "Go",
            "TypeScript"
        };

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { SkillSlugs = rawSkills },
            CancellationToken.None);
        var appliedTrendSkills = overview.TrendPoints
            .Select(point => point.SkillSlug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(["react", "sql", "docker", "python", "azure", "java"], appliedTrendSkills);
        Assert.DoesNotContain("go", appliedTrendSkills);
        Assert.Equal(6 * 30, overview.TrendPoints.Count);
    }

    [Fact]
    public async Task TC232_GetOverviewAsync_WithFourComparisonSkills_UsesOnlyFirstThreeAlignedPublicationSeries()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync(
            "topcv:232-1",
            fixture.Today,
            title: "React SQL Docker Python Engineer",
            skills: ["React", "SQL", "Docker", "Python"]);
        await fixture.AddHistoryCoverageAsync();

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                SkillSlugs = ["react", "sql", "docker", "python"]
            },
            CancellationToken.None);
        var analytics = overview.PublicationAnalytics;
        var comparisonSlugs = analytics.SkillComparisons.Select(item => item.SkillSlug).ToList();
        var seriesSlugs = analytics.SkillTrendPoints
            .Select(item => item.SkillSlug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(["react", "sql", "docker"], comparisonSlugs);
        Assert.Equal(comparisonSlugs, seriesSlugs);
        Assert.DoesNotContain("python", comparisonSlugs);
        Assert.Equal(3 * 30, analytics.SkillTrendPoints.Count);
        Assert.All(
            analytics.SkillComparisons,
            comparison => Assert.Equal(30, analytics.SkillTrendPoints.Count(point => point.SkillSlug == comparison.SkillSlug)));
    }

    [Fact]
    public async Task TC233_GetOverviewAsync_WithCombinedFilters_AppliesTheirIntersectionAcrossOverviewSections()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:233-match", fixture.Today, category: "Backend", location: "Ho Chi Minh", experience: "3 years", skills: ["React"]);
        await fixture.AddPostingAsync("topcv:233-category", fixture.Today, category: "Frontend", location: "Ho Chi Minh", experience: "3 years", skills: ["TypeScript"]);
        await fixture.AddPostingAsync("topcv:233-location", fixture.Today, category: "Backend", location: "Ha Noi", experience: "3 years", skills: ["Java"]);
        await fixture.AddPostingAsync("topcv:233-experience", fixture.Today, category: "Backend", location: "Ho Chi Minh", experience: "6 years", skills: ["Python"]);
        await fixture.AddHistoryCoverageAsync();

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                Category = " backend ",
                Location = "HO CHI MINH",
                Experience = "MID"
            },
            CancellationToken.None);

        Assert.Equal(1, overview.TotalPostings);
        Assert.Equal(1, overview.ActivePostings);
        Assert.Equal(1, overview.DataQuality.SampleSize);
        Assert.Equal(1, overview.InsightMeta.SampleSize);
        Assert.Equal(1, overview.PublicationAnalytics.PostDateQuality.SampleSize);
        Assert.Equal("Backend", Assert.Single(overview.CategorySummaries).Name);
        Assert.Equal("Ho Chi Minh", Assert.Single(overview.LocationSummaries).Name);
        Assert.Equal("Mid", Assert.Single(overview.ExperienceSummaries).Name);
        Assert.Equal("topcv:233-match", Assert.Single(overview.RecentJobs).Id);
        Assert.All(overview.SourceSummaries, source => Assert.Equal(1, source.Count));
    }

    [Fact]
    public async Task TC234_GetOverviewAsync_WithTopCvSourceInDifferentCasing_ReturnsSameTopCvOnlyOverview()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:234-1", fixture.Today, skills: ["React"]);
        await fixture.AddPostingAsync("topcv:234-2", fixture.Today.AddDays(-1), skills: ["SQL"]);
        await fixture.AddHistoryCoverageAsync();

        var lowerCase = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Source = "topcv" },
            CancellationToken.None);
        var mixedCase = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto { Source = "TopCV" },
            CancellationToken.None);

        Assert.Equal(lowerCase.TotalPostings, mixedCase.TotalPostings);
        Assert.Equal(lowerCase.ActivePostings, mixedCase.ActivePostings);
        Assert.Equal(lowerCase.PublicationAnalytics.CurrentPeriod.EstimatedTotal, mixedCase.PublicationAnalytics.CurrentPeriod.EstimatedTotal);
        Assert.Equal(lowerCase.RecentJobs.Select(item => item.Id), mixedCase.RecentJobs.Select(item => item.Id));
        Assert.All(lowerCase.SourceSummaries, source => Assert.Equal("TopCV", source.Name));
        Assert.All(mixedCase.SourceSummaries, source => Assert.Equal("TopCV", source.Name));
    }

    [Fact]
    public async Task TC235_GetOverview_WithUnsupportedSource_ReturnsUnsupportedSourceValidationEnvelope()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        var controller = new MarketPulseController(fixture.Service);

        var actionResult = await controller.GetOverview(
            source: "linkedin",
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        var envelope = Assert.IsType<MarketPulseApiEnvelopeDto<object>>(badRequest.Value);
        Assert.False(envelope.Ok);
        Assert.Null(envelope.Data);
        Assert.NotNull(envelope.Error);
        Assert.Equal("UNSUPPORTED_MARKET_PULSE_SOURCE", envelope.Error.Code);
        Assert.Contains("topcv", envelope.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC236_GetOverviewAsync_WithSalaryRange_IncludesOnlyOverlappingResolvableMonthlyRanges()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync(
            "topcv:236-normalized",
            fixture.Today,
            salary: "15 - 20 triệu",
            salaryMin: 15_000_000,
            salaryMax: 20_000_000,
            salaryCurrency: "VND");
        await fixture.AddPostingAsync("topcv:236-parsed", fixture.Today, salary: "25 - 35 triệu");
        await fixture.AddPostingAsync("topcv:236-below", fixture.Today, salary: "10 - 14 triệu");
        await fixture.AddPostingAsync("topcv:236-above", fixture.Today, salary: "31 - 40 triệu");
        await fixture.AddPostingAsync("topcv:236-unknown", fixture.Today, salary: "Negotiable");
        await fixture.AddHistoryCoverageAsync();

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                SalaryMinMonthlyVnd = 15_000_000,
                SalaryMaxMonthlyVnd = 30_000_000
            },
            CancellationToken.None);
        var includedIds = overview.RecentJobs.Select(item => item.Id).OrderBy(id => id).ToList();

        Assert.Equal(2, overview.TotalPostings);
        Assert.Equal(["topcv:236-normalized", "topcv:236-parsed"], includedIds);
        Assert.DoesNotContain(overview.RecentJobs, item => item.Id == "topcv:236-unknown");
        Assert.Equal(2, overview.DataQuality.SampleSize);
        Assert.Equal(2, overview.CategorySummaries.Sum(item => item.Count));
        Assert.Equal(2, overview.PublicationAnalytics.PostDateQuality.SampleSize);
    }

    [Fact]
    public async Task TC237_GetOverviewAsync_WithInvertedSalaryBounds_ReturnsSameResultAsNormalizedBounds()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:237-overlap-1", fixture.Today, salary: "15 - 20 triệu");
        await fixture.AddPostingAsync("topcv:237-overlap-2", fixture.Today, salary: "25 - 35 triệu");
        await fixture.AddPostingAsync("topcv:237-outside", fixture.Today, salary: "40 - 50 triệu");
        await fixture.AddHistoryCoverageAsync();

        var normalized = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                SalaryMinMonthlyVnd = 15_000_000,
                SalaryMaxMonthlyVnd = 30_000_000
            },
            CancellationToken.None);
        var inverted = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                SalaryMinMonthlyVnd = 30_000_000,
                SalaryMaxMonthlyVnd = 15_000_000
            },
            CancellationToken.None);

        Assert.Equal(normalized.TotalPostings, inverted.TotalPostings);
        Assert.Equal(normalized.ActivePostings, inverted.ActivePostings);
        Assert.Equal(normalized.RecentJobs.Select(item => item.Id), inverted.RecentJobs.Select(item => item.Id));
        Assert.Equal(normalized.CategorySummaries.Select(item => (item.Name, item.Count)), inverted.CategorySummaries.Select(item => (item.Name, item.Count)));
        Assert.Equal(normalized.SalaryInsight.SampleSize, inverted.SalaryInsight.SampleSize);
        Assert.Equal(normalized.PublicationAnalytics.CurrentPeriod.EstimatedTotal, inverted.PublicationAnalytics.CurrentPeriod.EstimatedTotal);
    }

    [Fact]
    public async Task TC238_GetOverviewAsync_WhenFiltersMatchNoPostings_ReturnsStableZeroValuedAnalytics()
    {
        await using var fixture = await MarketPulseOverviewTestFixture.CreateAsync();
        await fixture.AddPostingAsync("topcv:238-existing", fixture.Today, category: "Backend", location: "Ho Chi Minh", skills: ["React"]);
        await fixture.AddHistoryCoverageAsync();

        var overview = await fixture.Service.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                Category = "Data Science",
                Location = "Da Nang",
                Experience = "Senior",
                SkillSlugs = ["python"]
            },
            CancellationToken.None);

        Assert.Equal(0, overview.TotalPostings);
        Assert.Equal(0, overview.ActivePostings);
        Assert.Equal(0, overview.TodayPostings);
        Assert.Empty(overview.Skills);
        Assert.Empty(overview.CategorySummaries);
        Assert.Empty(overview.LocationSummaries);
        Assert.Empty(overview.ExperienceSummaries);
        Assert.Empty(overview.RecentJobs);
        Assert.Equal(0, overview.DataQuality.SampleSize);
        Assert.Contains(
            overview.DataQuality.Warnings,
            warning => warning.Contains("No active jobs", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0m, overview.PublicationAnalytics.CurrentPeriod.EstimatedTotal);
        Assert.Equal(0m, overview.PublicationAnalytics.PreviousPeriod.EstimatedTotal);
        Assert.All(
            overview.PublicationAnalytics.MarketTrendPoints,
            point => Assert.Equal(0m, point.TotalEstimate));
        Assert.Equal("flat", overview.PublicationAnalytics.MarketComparison.Direction);
    }

    [Fact]
    public void TC239_Build_WithKnownHistory_ReconcilesWeightsAndSuppressesOrReducesUnreliableClaims()
    {
        var anchor = new DateOnly(2026, 7, 22);
        const int days = 30;
        var currentStart = anchor.AddDays(-(days - 1));
        var previousStart = currentStart.AddDays(-days);
        var previousEnd = currentStart.AddDays(-1);
        var facts = Enumerable.Range(0, 50)
            .Select(index => new PublicationPostingFact(
                "exact",
                currentStart.AddDays(index % days),
                null,
                null,
                ["react"]))
            .Concat(Enumerable.Range(0, 50).Select(index => new PublicationPostingFact(
                "exact",
                previousStart.AddDays(index % days),
                null,
                null,
                ["react"])))
            .Append(new PublicationPostingFact(
                "relative",
                currentStart,
                currentStart.AddDays(-2),
                currentStart.AddDays(2),
                ["react"]))
            .ToList();

        var incompleteFacts = facts
            .Where(fact =>
                (fact.RepresentativeDate.HasValue && fact.RepresentativeDate.Value >= currentStart) ||
                (fact.UpperBound.HasValue && fact.UpperBound.Value >= currentStart))
            .ToList();

        var completeFresh = PublicationAnalyticsBuilder.Build(
            facts,
            anchor,
            DateTime.UtcNow,
            previousStart,
            anchor,
            days,
            ["react"]);
        var incompleteHistory = PublicationAnalyticsBuilder.Build(
            incompleteFacts,
            anchor,
            DateTime.UtcNow,
            currentStart,
            anchor,
            days,
            ["react"]);
        var staleHistory = PublicationAnalyticsBuilder.Build(
            facts,
            anchor,
            DateTime.UtcNow.AddDays(-5),
            previousStart,
            anchor,
            days,
            ["react"]);

        Assert.Equal("available", completeFresh.Availability);
        Assert.Equal("high", completeFresh.Confidence);
        Assert.Equal(days, completeFresh.CurrentPeriod.CoveredDays);
        Assert.Equal(days, completeFresh.PreviousPeriod.CoveredDays);
        Assert.Equal(
            101m,
            completeFresh.CurrentPeriod.EstimatedTotal + completeFresh.PreviousPeriod.EstimatedTotal,
            2);
        var react = Assert.Single(completeFresh.SkillComparisons);
        Assert.Equal(
            101m,
            react.CurrentTotal.GetValueOrDefault() + react.PreviousTotal.GetValueOrDefault(),
            2);
        Assert.NotEqual("insufficient", completeFresh.MarketComparison.Direction);

        Assert.Equal("insufficient_history", incompleteHistory.Availability);
        Assert.Equal(0, incompleteHistory.PreviousPeriod.CoveredDays);
        Assert.Equal("insufficient", incompleteHistory.MarketComparison.Direction);
        Assert.Null(incompleteHistory.MarketComparison.GrowthPercent);

        Assert.Equal("low", staleHistory.Confidence);
        Assert.Equal("low", staleHistory.MarketComparison.Confidence);
    }
}
