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
        CancellationToken cancellationToken = default)
    {
        var result = await marketPulseService.GetOverviewAsync(
            days,
            skills ?? [],
            cancellationToken);

        return Ok(result);
    }
}
