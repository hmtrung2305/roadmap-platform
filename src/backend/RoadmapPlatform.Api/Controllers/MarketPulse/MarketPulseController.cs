using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;

namespace RoadmapPlatform.Api.Controllers.MarketPulse;

[ApiController]
[Route("api/market-pulse")]
public sealed class MarketPulseController(IMarketPulseService marketPulseService) : ControllerBase
{
    [HttpGet("overview")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<MarketPulseOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] int days = 30,
        [FromQuery] string[]? skills = null,
        [FromQuery] string? category = null,
        [FromQuery] string? location = null,
        [FromQuery] string? experience = null,
        [FromQuery] string? source = null,
        [FromQuery] decimal? salaryMinMonthlyVnd = null,
        [FromQuery] decimal? salaryMaxMonthlyVnd = null,
        CancellationToken cancellationToken = default)
    {
        var result = await GetOverviewDtoAsync(
            days,
            skills,
            category,
            location,
            experience,
            source,
            salaryMinMonthlyVnd,
            salaryMaxMonthlyVnd,
            cancellationToken);

        return Ok(MarketPulseApiEnvelopeDto<MarketPulseOverviewDto>.Success(result));
    }

    [HttpGet("kpis")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<object>.Success(new
        {
            overview.TotalPostings,
            overview.ActivePostings,
            overview.TodayPostings,
            overview.SourceCount,
            overview.LastUpdatedAt,
            TopTrendingSkill = overview.Skills.FirstOrDefault(),
            MostActiveCategory = overview.CategorySummaries.FirstOrDefault(),
            overview.DataQuality
        }));
    }

    [HttpGet("trends/skills")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetSkillTrend(
        [FromQuery] int days = 30,
        [FromQuery] string[]? skills = null,
        CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(days: days, skills: skills, cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketTrendPointDto>>.Success(overview.TrendPoints));
    }

    [HttpGet("skills/top")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetTopSkills(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketSkillSummaryDto>>.Success(overview.Skills));
    }

    [HttpGet("distributions/categories")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetCategoryDistribution(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketSegmentSummaryDto>>.Success(overview.CategorySummaries));
    }

    [HttpGet("distributions/seniority")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetSeniorityDistribution(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketSegmentSummaryDto>>.Success(overview.ExperienceSummaries));
    }

    [HttpGet("distributions/locations")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetLocationDistribution(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketSegmentSummaryDto>>.Success(overview.LocationSummaries));
    }

    [HttpGet("insights")]
    [RequirePermission(PermissionConstant.MARKET_PULSE_VIEW_CATALOG)]
    public async Task<IActionResult> GetInsights(CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewDtoAsync(cancellationToken: cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<object>.Success(new
        {
            overview.InsightCards,
            overview.RisingSkills,
            overview.FallingSkills,
            overview.SkillCoOccurrences,
            overview.LearningRecommendations,
            overview.DataQuality,
            overview.InsightMeta
        }));
    }

    private Task<MarketPulseOverviewDto> GetOverviewDtoAsync(
        int days = 30,
        string[]? skills = null,
        string? category = null,
        string? location = null,
        string? experience = null,
        string? source = null,
        decimal? salaryMinMonthlyVnd = null,
        decimal? salaryMaxMonthlyVnd = null,
        CancellationToken cancellationToken = default) =>
        marketPulseService.GetOverviewAsync(
            new MarketPulseOverviewQueryDto
            {
                Days = days,
                SkillSlugs = skills ?? [],
                Category = category,
                Location = location,
                Experience = experience,
                Source = source,
                SalaryMinMonthlyVnd = salaryMinMonthlyVnd,
                SalaryMaxMonthlyVnd = salaryMaxMonthlyVnd
            },
            cancellationToken);
}
