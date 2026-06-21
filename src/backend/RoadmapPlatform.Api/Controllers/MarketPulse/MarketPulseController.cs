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
    [ProducesResponseType(typeof(MarketPulseOverviewDto), StatusCodes.Status200OK)]
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
        var result = await marketPulseService.GetOverviewAsync(
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

        return Ok(result);
    }
}
