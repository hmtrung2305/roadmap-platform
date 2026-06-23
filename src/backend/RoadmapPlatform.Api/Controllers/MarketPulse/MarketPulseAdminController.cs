using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;

namespace RoadmapPlatform.Api.Controllers.MarketPulse;

[ApiController]
[Route("api/market-pulse/admin")]
[RequirePermission(PermissionConstant.MARKET_PULSE_MANAGE_ANY)]
public sealed class MarketPulseAdminController(
    IMarketPulseService marketPulseService,
    IMarketPulseAdminService adminService) : ControllerBase
{
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var result = await marketPulseService.RefreshAsync(cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>.Success(result));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                MarketPulseApiEnvelopeDto<object>.Failure(
                    "MARKET_PULSE_REFRESH_RUNNING",
                    ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                MarketPulseApiEnvelopeDto<object>.Failure(
                    "MARKET_PULSE_REFRESH_FAILED",
                    "Manual refresh failed. Check crawl run history and failed items for details.",
                    new { message = ex.Message }));
        }
    }

    [HttpGet("crawl-runs")]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseCrawlRunDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrawlRuns(
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var runs = await adminService.GetCrawlRunsAsync(
            new MarketPulseAdminQueryDto
            {
                Status = status,
                Source = source,
                From = from,
                To = to,
                Limit = limit
            },
            cancellationToken);

        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseCrawlRunDto>>.Success(runs));
    }

    [HttpGet("failed-items")]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedItems(
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await adminService.GetFailedItemsAsync(
            new MarketPulseAdminQueryDto
            {
                Status = status,
                Source = source,
                From = from,
                To = to,
                Limit = limit
            },
            cancellationToken);

        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
    }

    [HttpPost("failed-items/{failedItemId:guid}/retry")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> RetryFailedItem(
        Guid failedItemId,
        CancellationToken cancellationToken)
    {
        var items = await adminService.RetryFailedItemsAsync([failedItemId], cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
    }

    [HttpPost("failed-items/retry")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> RetryFailedItems(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var items = await adminService.RetryFailedItemsAsync(request.FailedItemIds, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
    }

    [HttpPost("failed-items/{failedItemId:guid}/ignore")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> IgnoreFailedItem(
        Guid failedItemId,
        CancellationToken cancellationToken)
    {
        var items = await adminService.IgnoreFailedItemsAsync([failedItemId], cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
    }

    [HttpPost("failed-items/ignore")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> IgnoreFailedItems(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var items = await adminService.IgnoreFailedItemsAsync(request.FailedItemIds, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
    }

    [HttpGet("classifier/categories")]
    public async Task<IActionResult> GetClassifierCategories(CancellationToken cancellationToken)
    {
        var categories = await adminService.GetCategoriesAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<string>>.Success(categories));
    }

    [HttpGet("classifier/mappings")]
    public async Task<IActionResult> GetClassifierMappings(CancellationToken cancellationToken)
    {
        var mappings = await adminService.GetClassifierMappingsAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseClassifierMappingDto>>.Success(mappings));
    }

    [HttpPost("classifier/mappings")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> CreateClassifierMapping(
        [FromBody] MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var mapping = await adminService.CreateClassifierMappingAsync(request, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierMappingDto>.Success(mapping));
    }

    [HttpPut("classifier/mappings/{mappingId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> UpdateClassifierMapping(
        Guid mappingId,
        [FromBody] MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var mapping = await adminService.UpdateClassifierMappingAsync(mappingId, request, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierMappingDto>.Success(mapping));
    }

    [HttpDelete("classifier/mappings/{mappingId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> DeleteClassifierMapping(
        Guid mappingId,
        CancellationToken cancellationToken)
    {
        await adminService.DeleteClassifierMappingAsync(mappingId, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<object>.Success(new { deleted = true }));
    }

    [HttpPost("classifier/test")]
    public async Task<IActionResult> TestClassifier(
        [FromBody] MarketPulseClassifierTestRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.TestClassifierAsync(request, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierTestResultDto>.Success(result));
    }

    [HttpGet("source-health")]
    public async Task<IActionResult> GetSourceHealth(CancellationToken cancellationToken)
    {
        var health = await adminService.GetSourceHealthAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseSourceHealthDto>>.Success(health));
    }
}
